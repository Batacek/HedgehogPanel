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

/// <summary>End-to-end tests for the node endpoints (/api/nodes CRUD).</summary>
[Collection("IntegrationTests")]
public class NodeEndpointsTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public NodeEndpointsTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task GetNodes_WhenNotAuthenticated_IsRejected()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/api/nodes");

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Unauthorized,
            $"Expected redirect or 401 but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task CreateNode_WithValidData_ReturnsOkAndAppearsInList()
    {
        var client = await AuthenticatedClientAsync("node_create");

        var create = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-A", ipAddress = "10.0.0.1", port = 50051, description = "first" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        var list = await client.GetAsync("/api/nodes");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var body = await list.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.EnumerateArray().Select(e => e.GetProperty("name").GetString()).ToList();
        Assert.Contains("Node-A", names);
    }

    [Fact]
    public async Task CreateNode_WithDuplicateName_ReturnsBadRequest()
    {
        var client = await AuthenticatedClientAsync("node_dup");

        await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Dup", ipAddress = "10.0.0.2", port = 50051, description = (string?)null });

        var duplicate = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Dup", ipAddress = "10.0.0.3", port = 50052, description = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task UpdateNode_WhenExists_ReturnsOk()
    {
        var client = await AuthenticatedClientAsync("node_update");

        var create = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Upd", ipAddress = "10.0.0.4", port = 50051, description = (string?)null });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var update = await client.PutAsJsonAsync($"/api/nodes/{id}",
            new { name = "Node-Upd-2", ipAddress = "10.0.0.5", port = 50060, description = "changed" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task UpdateNode_WhenNotExists_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync("node_update_missing");

        var update = await client.PutAsJsonAsync($"/api/nodes/{Guid.NewGuid()}",
            new { name = "Ghost", ipAddress = "10.0.0.9", port = 50051, description = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [Fact]
    public async Task DeleteNode_WhenExists_ReturnsOk()
    {
        var client = await AuthenticatedClientAsync("node_delete");

        var create = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Del", ipAddress = "10.0.0.6", port = 50051, description = (string?)null });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var delete = await client.DeleteAsync($"/api/nodes/{id}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteNode_WhenNotExists_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync("node_delete_missing");

        var delete = await client.DeleteAsync($"/api/nodes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task GetNodes_JudgesTheProtocolVersionOfEachNode()
    {
        var client = await AuthenticatedClientAsync("node_compat");
        var connectionFactory = new EndpointTestConnectionFactory(_db.ConnectionString);
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = false } };
        var repository = new NodeRepository(connectionFactory, new InMemoryStore(new Mock<ILoggerService>().Object, config), config);

        await repository.CreateAsync(TestDataBuilder.CreateTestNode("Compat-Unpaired", "10.0.1.1", 50051));
        await repository.CreateAsync(PairedAt("Compat-Full", "10.0.1.2", "2.0.0"));
        await repository.CreateAsync(PairedAt("Compat-Limited", "10.0.1.3", "2.1.0"));
        await repository.CreateAsync(PairedAt("Compat-Incompatible", "10.0.1.4", "1.0.0"));

        var list = await client.GetAsync("/api/nodes");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var body = await list.Content.ReadFromJsonAsync<JsonElement>();
        string? Compatibility(string name) => body.EnumerateArray()
            .Single(e => e.GetProperty("name").GetString() == name)
            .GetProperty("compatibility").GetString();
        Assert.Null(Compatibility("Compat-Unpaired"));
        Assert.Equal("Full", Compatibility("Compat-Full"));
        Assert.Equal("Limited", Compatibility("Compat-Limited"));
        Assert.Equal("Incompatible", Compatibility("Compat-Incompatible"));
    }

    private static Node PairedAt(string name, string ipAddress, string protocolVersion) =>
        new(Guid.NewGuid(), name, ipAddress, 50051, status: NodeStatus.Online, daemonUuid: Guid.NewGuid(),
            daemonVersion: "0.1.0", protocolVersion: protocolVersion, daemonToken: "token");

    private async Task<HttpClient> AuthenticatedClientAsync(string prefix)
    {
        // Creating, updating and deleting nodes requires the Admin role.
        await _db.CleanDatabaseAsync();
        var admin = await EndpointTestSupport.SeedAdminAsync(_db.ConnectionString, prefix);
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, admin.Username);
        return client;
    }
}
