using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Regression test for the failed-attempt double-count bug: a single wrong password must increment
/// the lockout counter by exactly one, so the account locks only on the fifth failure (not the third).
/// Uses a dedicated factory so its rate-limit/lockout partition is isolated from other suites.
/// </summary>
[Collection("IntegrationTests")]
public class LoginFailureCountTests : IClassFixture<LoginFailureCountTests.FailureCountFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly FailureCountFactory _factory;

    public LoginFailureCountTests(PostgreSqlFixture db, FailureCountFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task WrongPassword_IncrementsCounterByExactlyOne_LockingOnTheFifthAttempt()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "double_count");
        var client = EndpointTestSupport.NewClient(_factory);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/login",
                new { username = user.Username, password = "definitely-wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var fifth = await client.PostAsJsonAsync("/api/login",
            new { username = user.Username, password = "definitely-wrong" });
        Assert.Equal(HttpStatusCode.Locked, fifth.StatusCode);
    }

    /// <summary>Dedicated factory so this class has its own lockout/rate-limit partition.</summary>
    public sealed class FailureCountFactory : HedgehogWebApplicationFactory
    {
    }
}
