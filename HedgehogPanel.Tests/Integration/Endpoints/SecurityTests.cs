using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>Cross-cutting security tests: CSP headers and JWT bearer authentication.</summary>
[Collection("IntegrationTests")]
public class SecurityTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly HedgehogWebApplicationFactory _factory;

    public SecurityTests(PostgreSqlFixture db, HedgehogWebApplicationFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task Response_IncludesContentSecurityPolicyHeader()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/html/login.html");

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        var csp = string.Join(" ", response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", csp);
    }

    [Fact]
    public async Task Response_IncludesHardeningHeaders()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.GetAsync("/html/login.html");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task JwtBearerToken_AuthenticatesProtectedEndpoint()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "jwt_user");

        var loginClient = EndpointTestSupport.NewClient(_factory);
        var login = await loginClient.PostAsJsonAsync("/api/login",
            new { username = user.Username, password = EndpointTestSupport.Password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        // Fresh client without the auth cookie — only the bearer token can authenticate it.
        var bearerClient = EndpointTestSupport.NewClient(_factory);
        bearerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await bearerClient.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.Username, body.GetProperty("username").GetString());
    }
}
