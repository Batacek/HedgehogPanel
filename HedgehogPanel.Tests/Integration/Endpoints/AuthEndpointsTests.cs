using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// End-to-end tests for the auth endpoints (/api/login, /api/logout, /api/me)
/// running the real application against the test PostgreSQL database.
/// </summary>
[Collection("IntegrationTests")]
public class AuthEndpointsTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public AuthEndpointsTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    // ----- /api/login -----

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "login_valid");
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login", new { username = user.Username, password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("HedgehogAuth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "login_badpass");
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login", new { username = user.Username, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingCredentials_ReturnsBadRequest()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login", new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        await _db.CleanDatabaseAsync();
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = "nobody_here", password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ----- /api/me -----

    [Fact]
    public async Task Me_WhenAuthenticated_ReturnsUserInfo()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "me_authed");
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.Username, body.GetProperty("username").GetString());
        Assert.False(body.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task Me_WhenNotAuthenticated_IsRejected()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/api/me");

        // Cookie auth challenges an unauthenticated API call with a redirect to the
        // login page; depending on configuration it may also surface as 401.
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Unauthorized,
            $"Expected redirect or 401 but got {(int)response.StatusCode}.");
    }

    // ----- /api/logout -----

    [Fact]
    public async Task Logout_WhenAuthenticated_ReturnsOk()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "logout_ok");
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var response = await client.PostAsync("/api/logout", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenMe_IsNoLongerAuthenticated()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "logout_clears");
        var client = EndpointTestSupport.NewClient(_factory);
        await EndpointTestSupport.LoginAsync(client, user.Username);

        var before = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        await client.PostAsync("/api/logout", content: null);

        var after = await client.GetAsync("/api/me");
        Assert.NotEqual(HttpStatusCode.OK, after.StatusCode);
    }
}
