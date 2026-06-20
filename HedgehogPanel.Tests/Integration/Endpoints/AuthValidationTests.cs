using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Input-validation branches of POST /api/login. These are rejected before any database
/// or authentication work, so no seeding is required. Uses its own factory instance so the
/// login rate limit is isolated from other endpoint tests.
/// </summary>
[Collection("IntegrationTests")]
public class AuthValidationTests : IClassFixture<HedgehogWebApplicationFactory>
{
    private readonly HedgehogWebApplicationFactory _factory;

    public AuthValidationTests(HedgehogWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithWhitespaceUsername_ReturnsBadRequest()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = "   ", password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidUsernameCharacters_ReturnsBadRequest()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = "bad user!", password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithTooLongUsername_ReturnsBadRequest()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = new string('a', 65), password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithTooLongPassword_ReturnsBadRequest()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        var response = await client.PostAsJsonAsync("/api/login",
            new { username = "validuser", password = new string('x', 257) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
