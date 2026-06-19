using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Endpoints;

/// <summary>
/// Verifies the account lockout flow end-to-end: repeated failed logins for the same
/// user lock the account and the login endpoint starts returning 423 Locked.
/// Uses a dedicated factory so its rate-limit/lockout state is isolated.
/// </summary>
[Collection("IntegrationTests")]
public class AccountLockoutEndpointTests : IClassFixture<AccountLockoutEndpointTests.LockoutFactory>
{
    private readonly PostgreSqlFixture _db;
    private readonly LockoutFactory _factory;

    public AccountLockoutEndpointTests(PostgreSqlFixture db, LockoutFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [Fact]
    public async Task RepeatedFailedLogins_LockTheAccount_Returning423()
    {
        await _db.CleanDatabaseAsync();
        var user = await EndpointTestSupport.SeedUserAsync(_db.ConnectionString, "lockout_user");
        var client = EndpointTestSupport.NewClient(_factory);

        var sawLocked = false;
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/login",
                new { username = user.Username, password = "definitely-wrong" });
            if (response.StatusCode == HttpStatusCode.Locked)
            {
                sawLocked = true;
                break;
            }
        }

        Assert.True(sawLocked, "Expected the account to be locked (423) after repeated failed logins.");
    }

    /// <summary>Dedicated factory so this class has its own lockout/rate-limit partition.</summary>
    public sealed class LockoutFactory : HedgehogWebApplicationFactory
    {
    }
}
