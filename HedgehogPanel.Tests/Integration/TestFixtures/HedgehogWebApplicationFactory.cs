using System;
using System.Threading.Tasks;
using HedgehogPanel;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

/// <summary>
/// Boots the real application in-memory against the test PostgreSQL database.
/// Database/JWT settings and the Development environment are exported as environment
/// variables in the static constructor — before the host is built — because
/// <c>ConfigLoader</c> reads them directly during startup and the cookie security
/// policy depends on the environment being Development (so cookies work over HTTP).
/// </summary>
public class HedgehogWebApplicationFactory : WebApplicationFactory<Program>
{
    static HedgehogWebApplicationFactory()
    {
        // Database credentials and the JWT secret are taken from the project's .env file; the
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DB_HOST", TestDatabaseConfig.Host);
        Environment.SetEnvironmentVariable("DB_PORT", TestDatabaseConfig.Port.ToString());
        Environment.SetEnvironmentVariable("DB_USER", TestDatabaseConfig.Username);
        Environment.SetEnvironmentVariable("DB_PASSWORD", TestDatabaseConfig.Password);
        Environment.SetEnvironmentVariable("DB_NAME", TestDatabaseConfig.Database);
        Environment.SetEnvironmentVariable("JWT_SECRET", TestDatabaseConfig.JwtSecret);
    }

    /// <summary>When true, antiforgery validation is replaced with a no-op so POST/PUT/DELETE
    /// endpoints can be exercised directly without juggling CSRF tokens.</summary>
    protected virtual bool DisableAntiforgery => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        if (DisableAntiforgery)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();
            });
        }
    }

    /// <summary>No-op antiforgery so state-changing requests skip CSRF validation in tests.</summary>
    private sealed class NoOpAntiforgery : IAntiforgery
    {
        private static AntiforgeryTokenSet Tokens =>
            new("test-request-token", "test-cookie-token", "__RequestVerificationToken", "X-CSRF-Token");

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => Tokens;
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => Tokens;
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }
}

/// <summary>
/// Variant of the factory that keeps the real antiforgery middleware active,
/// used to verify that CSRF protection rejects unprotected state-changing requests.
/// </summary>
public class AntiforgeryEnabledWebApplicationFactory : HedgehogWebApplicationFactory
{
    protected override bool DisableAntiforgery => false;
}
