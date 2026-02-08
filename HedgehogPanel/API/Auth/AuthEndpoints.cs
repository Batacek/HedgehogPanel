using System.Security.Claims;
using System.Text;
using HedgehogPanel.Core.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using HedgehogPanel.Core;
using HedgehogPanel.Core.Security;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Configuration;
using HedgehogPanel.Core.Store;
using Microsoft.AspNetCore.RateLimiting;

namespace HedgehogPanel.API.Auth;

public static class AuthEndpoints
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(AuthEndpoints));

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Auth endpoints...");
        
        // Login API
        endpoints.MapPost("/api/login", async (HttpContext ctx, LoginRequest req, IAccountLockoutService lockoutSvc, IAccountManager accountManager, IDataProvider dataProvider, HedgehogConfig config) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { error = "Missing username or password.", lockoutTimeRemaining = (string?)null });
            }
            var username = req.Username.Trim();
            var password = req.Password;
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (username.Length > 64 || password.Length > 256)
            {
                return Results.BadRequest(new { error = "Invalid credentials.", lockoutTimeRemaining = (string?)null });
            }
            foreach (var ch in username)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-'))
                {
                    return Results.BadRequest(new { error = "Invalid username format.", lockoutTimeRemaining = (string?)null });
                }
            }

            // Check lockout before authenticating
            if (await lockoutSvc.IsAccountLockedAsync(username, ip))
            {
                var remaining = await lockoutSvc.GetLockoutTimeRemainingAsync(username, ip);
                var mmss = remaining.HasValue ? FormatTime(remaining.Value) : null;
                Logger.Warning("Login attempt while locked out. User={Username}, IP={IP}, Remaining={Remaining}", username, ip, remaining);

                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Login.Blocked",
                    null,
                    null,
                    ip,
                    ctx.Request.Headers["User-Agent"],
                    false,
                    new { username, reason = "Account locked" }
                ));

                return Results.Json(new { error = "Account is temporarily locked due to multiple failed login attempts.", lockoutTimeRemaining = mmss }, statusCode: 423);
            }

            try
            {
                var account = await accountManager.AuthenticateAsync(username, password);
                
                // Warmup cache
                _ = Task.Run(async () => {
                    try {
                        await dataProvider.WarmupAsync(account.GUID);
                    } catch (Exception ex) {
                        Logger.Error(ex, "Cache warmup failed for user {User}", account.GUID);
                    }
                });

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, account.Name ?? username),
                    new Claim(ClaimTypes.NameIdentifier, account.Username),
                    new Claim("username", account.Username),
                    new Claim("guid", account.GUID.ToString())
                };
                if (account.IsAdmin)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                }

                // Issue cookie auth
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true
                };
                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

                // Reset failed attempts after success
                await lockoutSvc.ResetFailedAttemptsAsync(username, ip);
                Logger.Information("User {Username} authenticated successfully from {IP}.", username, ip);

                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Login.Success",
                    account.GUID,
                    null,
                    ip,
                    ctx.Request.Headers["User-Agent"],
                    true,
                    new { authMethod = "password" }
                ));

                // Additionally, generate JWT for API clients if configured
                string? token = null;
                try
                {
                    token = GenerateJwtToken(claims, config);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "JWT token generation skipped (misconfigured secret or other issue).");
                }

                return Results.Ok(new { success = true, token, lockoutTimeRemaining = (string?)null });
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Warning("Failed to authenticate user {Username} from {IP}.", username, ip);
                
                await Logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Login.Failed",
                    null,
                    null,
                    ip,
                    ctx.Request.Headers["User-Agent"],
                    false,
                    new { username, failureReason = "Invalid credentials", authMethod = "password" }
                ));

                await lockoutSvc.RecordFailedAttemptAsync(username, ip);
                // If now locked, return 423 with remaining
                if (await lockoutSvc.IsAccountLockedAsync(username, ip))
                {
                    var remaining = await lockoutSvc.GetLockoutTimeRemainingAsync(username, ip);
                    var mmss = remaining.HasValue ? FormatTime(remaining.Value) : null;
                    return Results.Json(new { error = "Account locked due to too many failed attempts.", lockoutTimeRemaining = mmss }, statusCode: 423);
                }
                // Otherwise standard unauthorized
                return Results.Json(new { error = "Invalid username or password.", lockoutTimeRemaining = (string?)null }, statusCode: 401);
            }
        })
        .AllowAnonymous()
        .RequireRateLimiting("LoginRateLimit");

        // Logout API
        endpoints.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            var username = ctx.User?.Identity?.Name ?? "Unknown";
            var guidClaim = ctx.User?.FindFirst("guid")?.Value;
            Guid? userGuid = guidClaim != null ? Guid.Parse(guidClaim) : null;
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Logger.Information("User {Username} logged out.", username);

            await Logger.LogSecurityEventAsync(new SecurityEvent(
                "User.Logout",
                userGuid,
                null,
                ip,
                ctx.Request.Headers["User-Agent"],
                true
            ));

            return Results.Ok(new { success = true });
        });

        // Who am I
        endpoints.MapGet("/api/me", (HttpContext ctx) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                var username = ctx.User.FindFirst("username")?.Value
                               ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? ctx.User.Identity?.Name
                               ?? string.Empty;
                var displayName = ctx.User.FindFirst(ClaimTypes.Name)?.Value
                                  ?? (!string.IsNullOrWhiteSpace(username) ? username : "User");
                var isAdmin = ctx.User.IsInRole("Admin") || ctx.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
                Logger.Debug("Retrieved info for user {Username}.", username);
                return Results.Ok(new { username, displayName, isAdmin });
            }
            Logger.Warning("Unauthenticated request to /api/me.");
            return Results.Unauthorized();
        }).RequireAuthorization();

        return endpoints;
    }

    private static string GenerateJwtToken(List<Claim> claims, HedgehogConfig config)
    {
        var secret = config.Auth.Jwt.Secret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            Logger.Error("JWT secret is not configured in appsettings.json or environment variables.");
            throw new InvalidOperationException("JWT secret not configured");
        }
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
        {
            Logger.Error("JWT secret must be at least 32 bytes (256 bits). Current length: {Length} bytes.", keyBytes.Length);
            throw new InvalidOperationException("JWT secret too short (min 32 bytes)");
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config.Auth.Jwt.Issuer,
            audience: config.Auth.Jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(config.Auth.Jwt.ExpiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool VerifyToken(string token, HedgehogConfig config)
    {
        try
        {
            var secret = config.Auth.Jwt.Secret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                Logger.Warning("JWT secret is not configured; token verification will fail.");
                return false;
            }
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            if (keyBytes.Length < 32)
            {
                Logger.Warning("JWT secret is too short ({Length} bytes); token verification will fail.", keyBytes.Length);
                return false;
            }
            var key = new SymmetricSecurityKey(keyBytes);
            var handler = new JwtSecurityTokenHandler();
            
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = config.Auth.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = config.Auth.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return validatedToken is JwtSecurityToken;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatTime(TimeSpan span)
    {
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
    }

    public record LoginRequest(string Username, string Password);
}