using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Verifies CSRF protection is enforced. Uses the factory variant that keeps the real
/// antiforgery middleware active (no no-op stub), so a state-changing request without a
/// valid token must be rejected.
/// </summary>
[Collection("IntegrationTests")]
public class AntiforgeryTests : IClassFixture<AntiforgeryEnabledWebApplicationFactory>
{
    private readonly AntiforgeryEnabledWebApplicationFactory _factory;

    public AntiforgeryTests(AntiforgeryEnabledWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_WithoutCsrfToken_IsRejected()
    {
        var client = EndpointTestSupport.NewClient(_factory);

        // No prior GET to obtain the XSRF-TOKEN cookie and no X-CSRF-Token header.
        var response = await client.PostAsJsonAsync("/api/login",
            new { username = "someone", password = EndpointTestSupport.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
