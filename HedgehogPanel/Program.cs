using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Serilog;
using HedgehogPanel.Managers;
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

        Log.Information("Starting Hedgehog Panel Web Application...");
        Log.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        Log.Information("Content Root Path: {ContentRootPath}", AppContext.BaseDirectory);
        Log.Information("Web Root Path: {WebRootPath}", Path.Combine(AppContext.BaseDirectory, "html"));
        
        Log.Information("Initializing builder...");

        var builder = null as WebApplicationBuilder;
        
        try
        {
            builder = WebApplication.CreateBuilder(args);
            Log.Information("Builder initialized.");
        } catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to initialize WebApplication builder.");
            return;
        }

        Log.Information("Configuring services...");
        Log.Information("Setting up authentication and authorization...");
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
        Log.Information("Authentication configured.");
        
        builder.Services.AddAuthorization();
        Log.Information("Authorization configured.");

        Log.Information("Building application...");
        var app = builder.Build();
        Log.Information("Application built.");
        
        app.UseAuthentication();
        app.UseAuthorization();

        // Protect HTML pages (except the login page and static assets) by redirecting unauthenticated users
        Log.Information("Configuring page protection...");
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
        Log.Information("Page protection configured.");
        
        Log.Information("Configuring endpoints...");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "html")),
            RequestPath = "/html"
        });
        Log.Information("Endpoints configured.");

        Log.Information("Mapping redirect for root path...");
        app.MapGet("/", (HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                return Results.File(Path.Combine(env.ContentRootPath, "html", "index.html"), "text/html; charset=utf-8");
            }
            return Results.Redirect("/html/login.html");
        });
        Log.Information("Redirect mapped.");

        Log.Information("Mapping API endpoints...");
        app.MapApi();
        Log.Information("API endpoints mapped.");

        Log.Information("Starting application...");
        app.Run();
    }
}