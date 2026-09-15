using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Infrastructure.Persistence.Store;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>End-to-end tests for GET /api/servers.</summary>
[Collection("IntegrationTests")]
public class ServerEndpointsTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public ServerEndpointsTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task GetServers_WhenNotAuthenticated_IsRejected()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/api/servers");

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Unauthorized,
            $"Expected redirect or 401 but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task GetServers_ReturnsServersOwnedByUser()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "srv_owner");

        var serverRepository = new ServerRepository(new EndpointTestConnectionFactory(_db.ConnectionString));
        var server = TestDataBuilder.CreateTestServer("MyServer", "my.hedgehog.batacek.eu");
        await serverRepository.CreateAsync(server);
        await serverRepository.AssignToUserAsync(server.Guid, user.Guid);

        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/servers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.EnumerateArray().Select(e => e.GetProperty("name").GetString()).ToList();
        Assert.Contains("MyServer", names);
    }

    [Fact]
    public async Task GetServers_WhenUserHasNoServers_ReturnsEmptyArray()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "srv_none");

        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/servers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Empty(body.EnumerateArray());
    }

    [Fact]
    public async Task GetServers_ReportsTheNodeRunningTheDaemon()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "srv_node");

        var connectionFactory = new EndpointTestConnectionFactory(_db.ConnectionString);
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = false } };
        var store = new InMemoryStore(new Mock<ILoggerService>().Object, config);
        var nodeRepository = new NodeRepository(connectionFactory, store, config);
        var node = TestDataBuilder.CreateTestNode("EndpointNode", "192.168.1.60", 50051);
        await nodeRepository.CreateAsync(node);

        var serverRepository = new ServerRepository(connectionFactory);
        var server = TestDataBuilder.CreateTestServer("NodedServer", "noded.hedgehog.batacek.eu", nodeUuid: node.Guid);
        await serverRepository.CreateAsync(server);
        await serverRepository.AssignToUserAsync(server.Guid, user.Guid);

        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/servers");

        // The nodes page joins on this field, so it has to reach the browser.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entry = body.EnumerateArray().Single(e => e.GetProperty("name").GetString() == "NodedServer");
        Assert.Equal(node.Guid.ToString(), entry.GetProperty("nodeUuid").GetString());
    }

    [Fact]
    public async Task GetServers_WithoutANode_ReportsNoLink()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "srv_nonode");

        var serverRepository = new ServerRepository(new EndpointTestConnectionFactory(_db.ConnectionString));
        var server = TestDataBuilder.CreateTestServer("BareServer", "bare.hedgehog.batacek.eu");
        await serverRepository.CreateAsync(server);
        await serverRepository.AssignToUserAsync(server.Guid, user.Guid);

        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/servers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entry = body.EnumerateArray().Single(e => e.GetProperty("name").GetString() == "BareServer");
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("nodeUuid").ValueKind);
    }
}
