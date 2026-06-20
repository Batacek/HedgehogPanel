using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>End-to-end tests for the admin endpoints (/api/admin/*), including the Admin role guard.</summary>
[Collection("IntegrationTests")]
public class AdminEndpointsTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public AdminEndpointsTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task AdminUsers_WhenNotAuthenticated_IsRejected()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/api/admin/users");

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Unauthorized,
            $"Expected redirect or 401 but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task AdminUsers_WhenAuthenticatedAsNonAdmin_ReturnsForbidden()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "plain_user");
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/admin/users");

        // An authenticated non-admin is denied. The cookie handler surfaces an authorization
        // failure as a redirect (to the access-denied/login path) rather than a bare 403,
        // so accept either — the key property is that access is not granted (no 200).
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected forbidden or redirect but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task AdminUsers_WhenAuthenticatedAsAdmin_ReturnsUserList()
    {
        var (client, admin) = await AdminClientAsync("admin_list");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var usernames = body.EnumerateArray().Select(e => e.GetProperty("username").GetString()).ToList();
        Assert.Contains(admin.Username, usernames);
    }

    [Fact]
    public async Task AdminCreateUser_WithValidData_ReturnsOk()
    {
        var (client, _) = await AdminClientAsync("admin_create");

        var newUsername = $"created_{Guid.NewGuid():N}".Substring(0, 24);
        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            username = newUsername,
            email = $"{newUsername}@hedgehog.batacek.eu",
            password = EndpointTestSupport.Password,
            firstName = "New",
            middleName = (string?)null,
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateUser_WithShortPassword_ReturnsBadRequest()
    {
        var (client, _) = await AdminClientAsync("admin_weakpass");

        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            username = $"weak_{Guid.NewGuid():N}".Substring(0, 20),
            email = "weak@hedgehog.batacek.eu",
            password = "short",
            firstName = (string?)null,
            middleName = (string?)null,
            lastName = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteUser_WhenExists_ReturnsOk()
    {
        var (client, _) = await AdminClientAsync("admin_del");
        var victim = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "victim");

        var response = await client.DeleteAsync($"/api/admin/users/{victim.Username}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteUser_BuiltInAdmin_ReturnsBadRequest()
    {
        var (client, _) = await AdminClientAsync("admin_protect");

        var response = await client.DeleteAsync("/api/admin/users/admin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateServer_WithName_ReturnsOk()
    {
        var (client, _) = await AdminClientAsync("admin_srv_create");

        var response = await client.PostAsJsonAsync("/api/admin/servers",
            new { name = "AdminServer", description = "via admin", ownerUsername = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreateServer_WithoutName_ReturnsBadRequest()
    {
        var (client, _) = await AdminClientAsync("admin_srv_noname");

        var response = await client.PostAsJsonAsync("/api/admin/servers",
            new { name = "", description = (string?)null, ownerUsername = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteServer_WhenExists_ReturnsOk()
    {
        var (client, _) = await AdminClientAsync("admin_srv_del");

        var serverRepository = new ServerRepository(new EndpointTestConnectionFactory(_db.ConnectionString));
        var server = TestDataBuilder.CreateTestServer("ToDelete", "todelete.hedgehog.batacek.eu");
        await serverRepository.CreateAsync(server);

        var response = await client.DeleteAsync($"/api/admin/servers/{server.Guid}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Cleans the DB, seeds an admin, logs in and returns the authenticated client + admin account.</summary>
    private async Task<(HttpClient client, HedgehogPanel.Domain.Entities.Account admin)> AdminClientAsync(string prefix)
    {
        await _db.CleanDatabaseAsync();
        var admin = await EndpointTestSupport.SeedAdminAsync(_db.ConnectionString, prefix);
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, admin.Username);
        return (client, admin);
    }
}
