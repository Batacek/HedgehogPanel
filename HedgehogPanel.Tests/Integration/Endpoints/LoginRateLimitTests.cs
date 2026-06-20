using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Verifies the per-IP login rate limit (LoginRateLimit policy, 10 requests / 5 min).
/// Uses a dedicated factory instance so the rate-limit counter is isolated from other tests.
/// Distinct usernames are used so the per-account lockout (423) is never triggered and only
/// the rate limiter (429) can fire.
/// </summary>
[Collection("IntegrationTests")]
public class LoginRateLimitTests : IClassFixture<LoginRateLimitTests.RateLimitFactory>
{
    private readonly RateLimitFactory _factory;

    public LoginRateLimitTests(RateLimitFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_AfterExceedingLimit_Returns429()
    {
        var client = EndpointTestSupport.NewClient(_factory);
        var sawTooManyRequests = false;

        for (var i = 0; i < 15; i++)
        {
            var response = await client.PostAsJsonAsync("/api/login",
                new { username = $"ratelimit_user_{i}", password = EndpointTestSupport.Password });
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                sawTooManyRequests = true;
                break;
            }
        }

        Assert.True(sawTooManyRequests, "Expected the login rate limiter to return 429 within 15 attempts.");
    }

    /// <summary>Dedicated factory so this class has its own rate-limit partition.</summary>
    public sealed class RateLimitFactory : HedgehogWebApplicationFactory
    {
    }
}
