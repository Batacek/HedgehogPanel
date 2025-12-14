using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Serilog;
using HedgehogPanel.API;

namespace HedgehogPanel;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    "logs/hedgehog-panel-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize logger: {ex}");
            return;
        }

        var logger = Log.ForContext<Program>();
        logger.Information("Starting Hedgehog Panel Web Application...");
        logger.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        logger.Information("Content Root Path: {ContentRootPath}", AppContext.BaseDirectory);
        logger.Information("Web Root Path: {WebRootPath}", Path.Combine(AppContext.BaseDirectory, "html"));
        
        logger.Information("Initializing builder...");

        var builder = null as WebApplicationBuilder;
        
        try
        {
            builder = WebApplication.CreateBuilder(args);
            logger.Information("Builder initialized.");
        } catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to initialize WebApplication builder.");
            return;
        }

        logger.Information("Configuring services...");
        logger.Information("Setting up authentication and authorization...");
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
        logger.Information("Authentication configured.");
        
        builder.Services.AddAuthorization();
        logger.Information("Authorization configured.");

        logger.Information("Building application...");
        var app = builder.Build();
        logger.Information("Application built.");
        
        app.UseAuthentication();
        // Log attempts to access Admin API by non-admins (before authorization short-circuits)
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                var isAdmin = isAuthenticated && (context.User.IsInRole("Admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"));
                if (!isAdmin)
                {
                    var username = context.User?.FindFirst("username")?.Value
                                   ?? context.User?.Identity?.Name
                                   ?? "Anonymous";
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    Log.ForContext<Program>()
                       .ForContext("Path", context.Request.Path.ToString())
                       .ForContext("IP", ip)
                       .Warning("Unauthorized access attempt to Admin API by {User}. Authenticated={Authenticated}", username, isAuthenticated);
                }
            }
            await next();
        });
        app.UseAuthorization();

        // Protect HTML pages (except the login page and static assets) by redirecting unauthenticated users
        logger.Information("Configuring page protection...");
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

                    // Block access to Admin page HTML for non-admin users
                    var isAdmin = context.User.IsInRole("Admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
                    if (!isAdmin)
                    {
                        if (path.Equals("/html/components/MainContent/Admin.html", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Forbidden: Admins only.");
                            return;
                        }
                    }
                }
            }

            await next();
        });
        logger.Information("Page protection configured.");
        
        logger.Information("Configuring endpoints...");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "html")),
            RequestPath = "/html"
        });
        logger.Information("Endpoints configured.");

        logger.Information("Mapping redirect for root path...");
        app.MapGet("/", (HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                return Results.File(Path.Combine(env.ContentRootPath, "html", "index.html"), "text/html; charset=utf-8");
            }
            return Results.Redirect("/html/login.html");
        });
        logger.Information("Redirect mapped.");

        logger.Information("Mapping API endpoints...");
        app.MapApi();
        logger.Information("API endpoints mapped.");

        logger.Information("Starting application...");
        app.Run();
    }
}