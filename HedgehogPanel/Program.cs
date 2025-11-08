using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HedgehogPanel.Managers;

namespace HedgehogPanel;

class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/html/login.html";
                options.Cookie.Name = "HedgehogAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.SlidingExpiration = true;
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        // Protect HTML pages (except the login page and static assets) by redirecting unauthenticated users
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            bool isApi = path.StartsWithSegments("/api");
            bool isLoginPage = path.Equals("/html/login.html", StringComparison.OrdinalIgnoreCase);
            bool isCss = path.StartsWithSegments("/html/css") || path.Value?.EndsWith(".css", StringComparison.OrdinalIgnoreCase) == true;
            bool isJs = path.StartsWithSegments("/html/js") || path.Value?.EndsWith(".js", StringComparison.OrdinalIgnoreCase) == true;
            bool isAsset = isCss || isJs || path.StartsWithSegments("/favicon.ico") || path.StartsWithSegments("/html/images") || path.StartsWithSegments("/html/assets");

            var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

            if (!isApi)
            {
                if (!isAuthenticated)
                {
                    if (path == "/" || (path.StartsWithSegments("/html") && !isLoginPage && !isAsset))
                    {
                        context.Response.Redirect("/html/login.html");
                        return;
                    }
                }
                else
                {
                    // If already authenticated and tries to access login page, go to home
                    if (isLoginPage)
                    {
                        context.Response.Redirect("/");
                        return;
                    }
                }
            }

            await next();
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "html")),
            RequestPath = "/html"
        });

        app.MapGet("/", (HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                return Results.File(Path.Combine(env.ContentRootPath, "html", "index.html"), "text/html; charset=utf-8");
            }
            return Results.Redirect("/html/login.html");
        });

        app.MapPost("/api/login", async (HttpContext ctx, LoginRequest req) =>
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
                if (!(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-' ))
                {
                    return Results.BadRequest(new { error = "Invalid username format." });
                }
            }

            try
            {
                var account = await Managers.AccountManager.AuthenticateAsync(username, password);
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
        app.MapPost("/api/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { success = true });
        });

        app.MapGet("/api/me", (HttpContext ctx) =>
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

        app.MapGet("/api/servers", async (HttpContext ctx) =>
        {
            var userGuidStr = ctx.User?.FindFirst("guid")?.Value;
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out var userGuid))
            {
                return Results.Ok(Array.Empty<object>());
            }

            try
            {
                var servers = await AccountManager.GetServerListAsync(userGuid);
                var serverList = new List<object>();
                foreach (var server in servers)
                {
                    serverList.Add(new {
                        id = server.GUID.ToString(),
                        name = server.Name,
                        owner = "You",        // current user is the owner per query
                        role = "Owner",        // simplified role (owner)
                        status = "Unknown"     // status not in DB yet
                    });
                }
                return Results.Ok(serverList);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GET /api/servers] Failed to load servers: {ex}");

                return Results.Ok(Array.Empty<object>());
            }
        });

        app.Run();
    }

    public record LoginRequest(string Username, string Password);
}