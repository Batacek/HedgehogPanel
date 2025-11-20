using System.Security.Claims;
using HedgehogPanel.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HedgehogPanel.API.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
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

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true
                };
                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
                return Results.Ok(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                // fall through to unauthorized result
            }
            return Results.Unauthorized();
        });

        // Logout API
        endpoints.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
                return Results.Ok(new { username, displayName });
            }
            return Results.Unauthorized();
        });

        return endpoints;
    }

    public record LoginRequest(string Username, string Password);
}