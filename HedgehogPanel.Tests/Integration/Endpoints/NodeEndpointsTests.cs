using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
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
            new { name = "Node-A", ipAddress = "10.0.0.1", port = 50051, description = "first", status = "Online", registrationToken = (string?)null });
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
            new { name = "Node-Dup", ipAddress = "10.0.0.2", port = 50051, description = (string?)null, status = (string?)null, registrationToken = (string?)null });

        var duplicate = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Dup", ipAddress = "10.0.0.3", port = 50052, description = (string?)null, status = (string?)null, registrationToken = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task UpdateNode_WhenExists_ReturnsOk()
    {
        var client = await AuthenticatedClientAsync("node_update");

        var create = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Upd", ipAddress = "10.0.0.4", port = 50051, description = (string?)null, status = (string?)null, registrationToken = (string?)null });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var update = await client.PutAsJsonAsync($"/api/nodes/{id}",
            new { name = "Node-Upd-2", ipAddress = "10.0.0.5", port = 50060, description = "changed", status = "Offline", registrationToken = (string?)null });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task UpdateNode_WhenNotExists_ReturnsNotFound()
    {
        var client = await AuthenticatedClientAsync("node_update_missing");

        var update = await client.PutAsJsonAsync($"/api/nodes/{Guid.NewGuid()}",
            new { name = "Ghost", ipAddress = "10.0.0.9", port = 50051, description = (string?)null, status = (string?)null, registrationToken = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [Fact]
    public async Task DeleteNode_WhenExists_ReturnsOk()
    {
        var client = await AuthenticatedClientAsync("node_delete");

        var create = await client.PostAsJsonAsync("/api/nodes",
            new { name = "Node-Del", ipAddress = "10.0.0.6", port = 50051, description = (string?)null, status = (string?)null, registrationToken = (string?)null });
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

    private async Task<HttpClient> AuthenticatedClientAsync(string prefix)
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, prefix);
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);
        return client;
    }
}
