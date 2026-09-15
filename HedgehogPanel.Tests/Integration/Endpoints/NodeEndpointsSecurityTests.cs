using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
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
/// End-to-end tests for what the node endpoints must not do: hand out daemon tokens,
/// let a client set pairing state, or let non-admins change nodes.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="NodeEndpointsTests"/> because each test class gets its own
/// application, and login there is rate limited to ten attempts per five minutes. Keep
/// the logins in this class under that limit.
/// </remarks>
[Collection("IntegrationTests")]
public class NodeEndpointsSecurityTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private const string DaemonToken = "daemon-token-that-must-stay-on-the-server";

    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public NodeEndpointsSecurityTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task GetNodes_NeverExposesTheDaemonToken()
    {
        await _db.CleanDatabaseAsync();
        await SeedPairedNodeAsync("Node-Secret", "10.0.0.17");
        var admin = await AdminClientAsync("node_secret_admin");
        await ClearCachedNodeListAsync(admin);
        var client = await UserClientAsync("node_secret_plain");

        var response = await client.GetAsync("/api/nodes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(DaemonToken, raw);

        // Found by name, so the test cannot pass just because the node was missing.
        var entry = JsonDocument.Parse(raw).RootElement.EnumerateArray()
            .Single(e => e.GetProperty("name").GetString() == "Node-Secret");
        Assert.True(entry.GetProperty("paired").GetBoolean());
        Assert.False(entry.TryGetProperty("daemonToken", out _));
        Assert.False(entry.TryGetProperty("registrationToken", out _));
    }

    [Fact]
    public async Task GetNodes_ReportsDaemonState()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-State", "10.0.0.19");
        var admin = await AdminClientAsync("node_state");
        await ClearCachedNodeListAsync(admin);

        var entry = await FindNodeAsync(admin, "Node-State");

        Assert.Equal("Online", entry.GetProperty("status").GetString());
        Assert.Equal(node.DaemonUuid!.Value.ToString(), entry.GetProperty("daemonUuid").GetString());
        Assert.Equal("0.1.0", entry.GetProperty("daemonVersion").GetString());
        Assert.Equal("2.0.0", entry.GetProperty("protocolVersion").GetString());
    }

    [Fact]
    public async Task CreateNode_StartsUnpaired()
    {
        await _db.CleanDatabaseAsync();
        var client = await AdminClientAsync("node_unpaired");

        await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-New", ipAddress = "10.0.0.11", port = 50051, description = (string?)null });

        var entry = await FindNodeAsync(client, "Node-New");
        Assert.Equal("Unpaired", entry.GetProperty("status").GetString());
        Assert.False(entry.GetProperty("paired").GetBoolean());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("daemonUuid").ValueKind);
    }

    [Fact]
    public async Task CreateNode_IgnoresStatusAndTokenSentByTheClient()
    {
        // A caller must not be able to mark a node online or plant a token of its choosing.
        await _db.CleanDatabaseAsync();
        var client = await AdminClientAsync("node_forged");

        await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Forged", ipAddress = "10.0.0.12", port = 50051, status = "Online", registrationToken = "forged", daemonToken = "forged" });

        var entry = await FindNodeAsync(client, "Node-Forged");
        Assert.Equal("Unpaired", entry.GetProperty("status").GetString());
        Assert.False(entry.GetProperty("paired").GetBoolean());
    }

    [Fact]
    public async Task CreateNode_AsNonAdmin_IsForbidden()
    {
        await _db.CleanDatabaseAsync();
        var client = await UserClientAsync("node_create_plain");

        var response = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Plain", ipAddress = "10.0.0.13", port = 50051, description = (string?)null });

        AssertForbidden(response);
    }

    [Fact]
    public async Task UpdateNode_KeepsPairingState()
    {
        // Same name on purpose: an ordinary edit leaves the name alone, and so will
        // every update pairing and health checks make.
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Paired", "10.0.0.15");
        var client = await AdminClientAsync("node_keep");

        var update = await client.PutAsJsonAsync($"/api/nodes/{node.Guid}",
            new { name = "Node-Paired", ipAddress = "10.0.0.15", port = 50051, description = "edited" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.NotNull(stored);
        Assert.Equal("edited", stored.Description);
        Assert.Equal(NodeStatus.Online, stored.Status);
        Assert.Equal(node.DaemonUuid, stored.DaemonUuid);
        Assert.Equal("2.0.0", stored.ProtocolVersion);
        Assert.Equal(DaemonToken, stored.DaemonToken);
    }

    [Fact]
    public async Task UpdateNode_AsNonAdmin_IsForbidden()
    {
        // Repointing a paired node's address would send its token wherever the caller chose.
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Target", "10.0.0.14");
        var client = await UserClientAsync("node_update_plain");

        var response = await client.PutAsJsonAsync($"/api/nodes/{node.Guid}",
            new { name = "Node-Target", ipAddress = "203.0.113.66", port = 50051, description = (string?)null });

        AssertForbidden(response);
        var stored = await DirectNodeRepository().GetByGuidAsync(node.Guid);
        Assert.Equal("10.0.0.14", stored!.IpAddress);
    }

    [Fact]
    public async Task DeleteNode_AsNonAdmin_IsForbidden()
    {
        await _db.CleanDatabaseAsync();
        var node = await SeedPairedNodeAsync("Node-Keep", "10.0.0.16");
        var client = await UserClientAsync("node_delete_plain");

        var response = await client.DeleteAsync($"/api/nodes/{node.Guid}");

        AssertForbidden(response);
        Assert.NotNull(await DirectNodeRepository().GetByGuidAsync(node.Guid));
    }

    private NodeRepository DirectNodeRepository()
    {
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = false } };
        var store = new InMemoryStore(new Mock<ILoggerService>().Object, config);
        return new NodeRepository(new EndpointTestConnectionFactory(_db.ConnectionString), store, config);
    }

    /// <summary>
    /// Inserts a paired node straight into the database, bypassing the application, so the
    /// application has never cached it and has to read the stored daemon state back.
    /// </summary>
    private async Task<Node> SeedPairedNodeAsync(string name, string ipAddress)
    {
        var node = new Node(Guid.NewGuid(), name, ipAddress, 50051,
            status: NodeStatus.Online, daemonUuid: Guid.NewGuid(), daemonVersion: "0.1.0",
            protocolVersion: "2.0.0", daemonToken: DaemonToken);
        await DirectNodeRepository().CreateAsync(node);
        return node;
    }

    /// <summary>
    /// Creating a node through the API drops the application's cached node list, so a node
    /// seeded directly into the database shows up in the next GET instead of being missed.
    /// </summary>
    private static async Task ClearCachedNodeListAsync(HttpClient adminClient)
    {
        var response = await adminClient.PostAsJsonAsync("/api/nodes",
            new { name = $"Node-Cache-{Guid.NewGuid():N}", ipAddress = "10.0.0.250", port = 50051, description = (string?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<JsonElement> FindNodeAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync("/api/nodes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.EnumerateArray().Single(e => e.GetProperty("name").GetString() == name);
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
