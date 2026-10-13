using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Infrastructure.Persistence.Store;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// End-to-end tests for pairing, ping and health: what reaches the daemon, what is stored on
/// the node, and which HTTP status each daemon failure becomes.
/// </summary>
/// <remarks>
/// Login is rate limited to ten attempts per five minutes for each application, and this
/// class gets an application of its own. Keep the logins in this class under that limit.
/// </remarks>
[Collection("IntegrationTests")]
public class NodeDaemonEndpointsTests : IClassFixture<FakeDaemonWebApplicationFactory>
{
    private const string Code = "ABCD-EFGH";
    private const string IssuedToken = "issued-token-that-must-stay-on-the-server";

    private readonly PostgreSqlFixture _db;
    private readonly FakeDaemonWebApplicationFactory _factory;

    public NodeDaemonEndpointsTests(PostgreSqlFixture db, FakeDaemonWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
        _factory.Daemon.Reset();
    }

    [Fact]
    public async Task Register_PairsTheNodeAndKeepsTheTokenOnTheServer()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedNodeAsync("Node-Pair", "10.0.1.1");
        var daemonUuid = Guid.NewGuid();
        _factory.Daemon.Setup(d => d.ProbeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonProbe("2.0.0", ProtocolCompatibility.Full, TimeSpan.FromSeconds(5)));
        _factory.Daemon.Setup(d => d.RegisterAsync(It.Is<Node>(n => n.Guid == node.Guid), Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(daemonUuid, IssuedToken));
        var client = await AdminClientAsync("pair_ok");

        var response = await client.PostAsJsonAsync($"/api/nodes/{node.Guid}/register", new { code = Code });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(IssuedToken, raw);
        var body = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("Online", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("paired").GetBoolean());
        Assert.Equal(daemonUuid.ToString(), body.GetProperty("daemonUuid").GetString());
        Assert.Equal("2.0.0", body.GetProperty("protocolVersion").GetString());
        Assert.Equal("Full", body.GetProperty("compatibility").GetString());

        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(IssuedToken, stored!.DaemonToken);
        Assert.Equal(daemonUuid, stored.DaemonUuid);
        Assert.Equal(NodeStatus.Online, stored.Status);
        Assert.NotNull(stored.LastSeen);

        // The node list the browser reads shows the pairing, still without the token.
        var list = await client.GetStringAsync("/api/nodes");
        Assert.DoesNotContain(IssuedToken, list);
    }

    [Fact]
    public async Task Register_WithAWrongCode_Returns400AndStoresNothing()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedNodeAsync("Node-Wrong", "10.0.1.2");
        _factory.Daemon.Setup(d => d.ProbeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonProbe("2.0.0", ProtocolCompatibility.Full, TimeSpan.FromSeconds(5)));
        _factory.Daemon.Setup(d => d.RegisterAsync(It.IsAny<Node>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(DaemonFailure.InvalidOneTimeCode, "The one-time code is wrong, expired or already used."));
        var client = await AdminClientAsync("pair_wrong");

        var response = await client.PostAsJsonAsync($"/api/nodes/{node.Guid}/register", new { code = "WXYZ-WXYZ" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InvalidOneTimeCode", body.GetProperty("failure").GetString());
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(NodeStatus.Unpaired, stored!.Status);
        Assert.Null(stored.DaemonToken);
    }

    [Fact]
    public async Task Register_AsNonAdmin_IsForbiddenAndContactsNothing()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedNodeAsync("Node-Plain", "10.0.1.3");
        var client = await UserClientAsync("pair_plain");

        var response = await client.PostAsJsonAsync($"/api/nodes/{node.Guid}/register", new { code = Code });

        AssertForbidden(response);
        _factory.Daemon.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Register_BadRequests_AreRejectedBeforeTheDaemon()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedNodeAsync("Node-Bad", "10.0.1.4");
        var client = await AdminClientAsync("pair_bad");

        var unknown = await client.PostAsJsonAsync($"/api/nodes/{Guid.NewGuid()}/register", new { code = Code });
        var malformedId = await client.PostAsJsonAsync("/api/nodes/not-a-guid/register", new { code = Code });
        var emptyCode = await client.PostAsJsonAsync($"/api/nodes/{node.Guid}/register", new { code = "  " });

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyCode.StatusCode);
        _factory.Daemon.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ping_StoresWhatTheDaemonReported()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Ping", "10.0.1.5");
        _factory.Daemon.Setup(d => d.HandshakeAsync(It.Is<Node>(n => n.Guid == node.Guid), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonHandshake(node.DaemonUuid!.Value, "0.2.0", "2.1.0", ProtocolCompatibility.Limited));
        var client = await AdminClientAsync("ping_ok");

        var response = await client.PostAsync($"/api/nodes/{node.Guid}/ping", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Online", body.GetProperty("status").GetString());
        Assert.Equal("Limited", body.GetProperty("compatibility").GetString());
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(NodeStatus.Online, stored!.Status);
        Assert.Equal("0.2.0", stored.DaemonVersion);
        Assert.Equal("2.1.0", stored.ProtocolVersion);
        Assert.Null(stored.LastError);
    }

    [Fact]
    public async Task Ping_Failures_AreStoredAndNeverLookLikeTheUsersOwnAuth()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Down", "10.0.1.6");
        var client = await AdminClientAsync("ping_fail");

        _factory.Daemon.Setup(d => d.HandshakeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(DaemonFailure.Unreachable, "Daemon at http://10.0.1.6:50051 is unreachable: connection refused"));
        var unreachable = await client.PostAsync($"/api/nodes/{node.Guid}/ping", null);

        Assert.Equal(HttpStatusCode.BadGateway, unreachable.StatusCode);
        var body = await unreachable.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unreachable", body.GetProperty("failure").GetString());
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(NodeStatus.Offline, stored!.Status);
        Assert.Contains("unreachable", stored.LastError);

        // A daemon rejecting the panel's token must not answer 401 or 403, which the
        // browser would take as its own session having ended.
        _factory.Daemon.Setup(d => d.HandshakeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(DaemonFailure.Unauthenticated, "Daemon at http://10.0.1.6:50051 rejected the credentials: bad token"));
        var rejected = await client.PostAsync($"/api/nodes/{node.Guid}/ping", null);

        Assert.Equal(HttpStatusCode.BadGateway, rejected.StatusCode);
        stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(NodeStatus.Unknown, stored!.Status);
    }

    [Fact]
    public async Task Ping_AnUnpairedNode_Returns409()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedNodeAsync("Node-Lonely", "10.0.1.7");
        _factory.Daemon.Setup(d => d.HandshakeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(DaemonFailure.NotPaired, "Node is not paired."));
        var client = await AdminClientAsync("ping_unpaired");

        var response = await client.PostAsync($"/api/nodes/{node.Guid}/ping", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal(NodeStatus.Unpaired, stored!.Status);
        Assert.Null(stored.LastError);
    }

    [Fact]
    public async Task Health_ReturnsTheHostsLoad()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Health", "10.0.1.8");
        _factory.Daemon.Setup(d => d.GetHealthAsync(It.Is<Node>(n => n.Guid == node.Guid), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonHealth(12.5, 1_000, 4_000, 87, TimeSpan.FromHours(3)));
        var client = await AdminClientAsync("health_ok");

        var response = await client.GetAsync($"/api/nodes/{node.Guid}/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12.5, body.GetProperty("cpuUsagePercent").GetDouble());
        Assert.Equal(1_000, body.GetProperty("ramUsedBytes").GetInt64());
        Assert.Equal(4_000, body.GetProperty("ramTotalBytes").GetInt64());
        Assert.Equal(87, body.GetProperty("runningProcesses").GetInt32());
        Assert.Equal(10_800, body.GetProperty("uptimeSeconds").GetInt64());
    }

    [Fact]
    public async Task Health_AsNonAdmin_IsForbiddenAndContactsNothing()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Hidden", "10.0.1.9");
        var client = await UserClientAsync("health_plain");

        var response = await client.GetAsync($"/api/nodes/{node.Guid}/health");

        AssertForbidden(response);
        _factory.Daemon.VerifyNoOtherCalls();
    }

    private NodeRepository DirectNodeRepository()
    {
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = false } };
        var store = new InMemoryStore(new Mock<ILoggerService>().Object, config);
        return new NodeRepository(new EndpointTestConnectionFactory(_db.ConnectionString), store, config);
    }

    private async Task<Node> SeedNodeAsync(string name, string ipAddress)
    {
        var node = new Node(Guid.NewGuid(), name, ipAddress, 50051);
        await DirectNodeRepository().CreateAsync(node);
        return node;
    }

    private async Task<Node> SeedPairedNodeAsync(string name, string ipAddress)
    {
        var node = new Node(Guid.NewGuid(), name, ipAddress, 50051,
            status: NodeStatus.Online, daemonUuid: Guid.NewGuid(), daemonVersion: "0.1.0",
            protocolVersion: "2.0.0", daemonToken: "stored-token");
        await DirectNodeRepository().CreateAsync(node);
        return node;
    }

    private async Task<HttpClient> AdminClientAsync(string prefix)
    {
        var admin = await EndpointTestSupport.SeedAdminAsync(_db.ConnectionString, prefix);
        return await LoggedInAsync(admin.Username);
    }

    private async Task<HttpClient> UserClientAsync(string prefix)
    {
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, prefix);
        return await LoggedInAsync(user.Username);
    }

    private async Task<HttpClient> LoggedInAsync(string username)
    {
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, username);
        return client;
    }

    private static void AssertForbidden(HttpResponseMessage response)
    {
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected 403 or redirect but got {(int)response.StatusCode}.");
    }
}
