using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Validation and additional read paths of the admin endpoints. Uses its own factory
/// instance so its login rate limit is isolated from AdminEndpointsTests.
/// </summary>
[Collection("IntegrationTests")]
public class AdminValidationTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public AdminValidationTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var client = await AdminClientAsync("adminval_dupemail");
        var email = $"dup_{Guid.NewGuid():N}@hedgehog.batacek.eu";

        var first = await client.PostAsJsonAsync("/api/admin/users", new
        {
            username = $"first_{Guid.NewGuid():N}".Substring(0, 20),
            email,
            password = EndpointTestSupport.Password,
            firstName = (string?)null, middleName = (string?)null, lastName = (string?)null
        });
        first.EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync("/api/admin/users", new
        {
            username = $"second_{Guid.NewGuid():N}".Substring(0, 20),
            email,
            password = EndpointTestSupport.Password,
            firstName = (string?)null, middleName = (string?)null, lastName = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidUsernameCharacters_ReturnsBadRequest()
    {
        var client = await AdminClientAsync("adminval_badname");

        var response = await client.PostAsJsonAsync("/api/admin/users", new
        {
            username = "bad name!",
            email = "x@hedgehog.batacek.eu",
            password = EndpointTestSupport.Password,
            firstName = (string?)null, middleName = (string?)null, lastName = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnlockUser_ReturnsOk()
    {
        var client = await AdminClientAsync("adminval_unlock");

        var response = await client.PostAsJsonAsync("/api/admin/users/someuser/unlock",
            new { clientIp = "1.2.3.4" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetServers_AsAdmin_ReturnsOk()
    {
        var client = await AdminClientAsync("adminval_servers");

        var response = await client.GetAsync("/api/admin/servers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpClient> AdminClientAsync(string prefix)
    {
        await _db.CleanDatabaseAsync();
        var admin = await EndpointTestSupport.SeedAdminAsync(_db.ConnectionString, prefix);
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, admin.Username);
        return client;
    }
}
