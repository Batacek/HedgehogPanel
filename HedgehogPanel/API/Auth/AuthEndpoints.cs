using System.Security.Claims;
using System.Text;
using HedgehogPanel.Core.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Serilog;
using HedgehogPanel.Core;

namespace HedgehogPanel.API.Auth;

public static class AuthEndpoints
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(AuthEndpoints));

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Logger.Information("Mapping Auth endpoints...");
        
        // Login API
        endpoints.MapPost("/api/login", async (HttpContext ctx, LoginRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new { error = "Missing username or password." });
            }
            var username = req.Username.Trim();
            var password = req.Password;

            if (username.Length > 64 || password.Length > 256)
            {
                return Results.BadRequest(new { error = "Invalid credentials." });
            }
            foreach (var ch in username)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-'))
                {
                    return Results.BadRequest(new { error = "Invalid username format." });
                }
            }

            try
            {
                var account = await AccountManager.AuthenticateAsync(username, password);
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

                // Additionally, generate JWT for API clients if configured
                string? token = null;
                try
                {
                    token = GenerateJwtToken(claims);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "JWT token generation skipped (misconfigured secret or other issue).");
                }

                Logger.Information("User {Username} authenticated successfully.", username);
                return Results.Ok(new { success = true, token });
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Warning("Failed to authenticate user {Username}.", username);
                // fall through to unauthorized result
            }
            return Results.Unauthorized();
        }).AllowAnonymous();

        // Logout API
        endpoints.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Logger.Information("User {Username} logged out.", ctx.User?.Identity?.Name ?? "Unknown");
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

    private static string GenerateJwtToken(List<Claim> claims)
    {
        var secret = Config.JwtSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            Logger.Error("JWT_SECRET is not configured. Add JWT_SECRET to your .env file.");
            throw new InvalidOperationException("JWT secret not configured");
        }
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
        {
            Logger.Error("JWT_SECRET must be at least 32 bytes (256 bits). Current length: {Length} bytes.", keyBytes.Length);
            throw new InvalidOperationException("JWT secret too short (min 32 bytes)");
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Config.JwtIssuer,
            audience: Config.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool VerifyToken(string token)
    {
        try
        {
            var secret = Config.JwtSecret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                Logger.Warning("JWT_SECRET is not configured; token verification will fail.");
                return false;
            }
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            if (keyBytes.Length < 32)
            {
                Logger.Warning("JWT_SECRET is too short ({Length} bytes); token verification will fail.", keyBytes.Length);
                return false;
            }
            var key = new SymmetricSecurityKey(keyBytes);
            var handler = new JwtSecurityTokenHandler();
            
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = Config.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = Config.JwtAudience,
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

    public record LoginRequest(string Username, string Password);
}