using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Serilog;
using HedgehogPanel.API;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Database;
using HedgehogPanel.Core.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Npgsql;

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

        var logger = HedgehogLogger.ForContext<Program>();
        logger.Information("Starting Hedgehog Panel Web Application...");
        logger.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        logger.Information("Content Root Path: {ContentRootPath}", AppContext.BaseDirectory);
        logger.Information("Web Root Path: {WebRootPath}", Path.Combine(AppContext.BaseDirectory, "html"));
        
        logger.Information("Initializing builder...");

        var builder = WebApplication.CreateBuilder(args);
        logger.Information("Builder initialized.");
        builder.Host.UseSerilog();
        try
        {
            Env.Load();
            logger.Information("Loaded environment variables from .env (if present).");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to load .env file; proceeding with process environment variables only.");
        }

        logger.Information("Configuring services...");

        // Database configuration
        var dbHost = Env.GetString("DB_HOST", "localhost");
        var dbPort = Env.GetInt("DB_PORT", 5432);
        var dbUser = Env.GetString("DB_USER") ?? throw new InvalidOperationException("DB_USER must be set.");
        var dbPass = Env.GetString("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD must be set.");
        var dbName = Env.GetString("DB_NAME") ?? throw new InvalidOperationException("DB_NAME must be set.");
        var connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPass};Database={dbName}";

        builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
        builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        builder.Services.AddSingleton<DatabaseLoggerService>();
        builder.Services.AddSingleton<IAccountManager, AccountManager>(sp => 
            new AccountManager(HedgehogLogger.ForContext<AccountManager>(), sp.GetRequiredService<IDbConnectionFactory>()));
        builder.Services.AddSingleton<IServerManager, ServerManager>(sp => 
            new ServerManager(HedgehogLogger.ForContext<ServerManager>(), sp.GetRequiredService<IDbConnectionFactory>()));
        
        logger.Information("Database services and managers registered.");

        // Rate Limiting configuration
        builder.Services.AddRateLimiter(options =>
        {
            // 429 for rejected
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var hedgehogLogger = HedgehogLogger.ForContext<Program>();
                await hedgehogLogger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Login.RateLimited",
                    null,
                    null,
                    ip,
                    context.HttpContext.Request.Headers["User-Agent"],
                    false,
                    new { signal = "Rate limit exceeded" }
                ));

                context.HttpContext.Response.ContentType = "application/json";
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra) ? ra.TotalSeconds : (double?)null;
                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.Value).ToString();
                }
                var payload = System.Text.Json.JsonSerializer.Serialize(new { error = "Too many requests. Please try again later." });
                await context.HttpContext.Response.WriteAsync(payload, token);
            };

            // Global fallback limiter (per-IP)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter($"global:{ip}", _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            // Named policy for login endpoint
            options.AddPolicy("LoginRateLimit", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter($"login:{ip}", _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10, // 10 attempts per 5 minutes
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        // Caching + Lockout service
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<HedgehogPanel.Core.Security.IAccountLockoutService, HedgehogPanel.Core.Security.AccountLockoutService>();

        logger.Information("Setting up authentication and authorization...");
        var jwtSecret = HedgehogPanel.Core.Config.JwtSecret;
        var jwtKeyBytes = !string.IsNullOrWhiteSpace(jwtSecret) ? Encoding.UTF8.GetBytes(jwtSecret) : Array.Empty<byte>();
        var jwtEnabled = jwtKeyBytes.Length >= 32;

        if (jwtEnabled)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "AppAuth";
                options.DefaultChallengeScheme = "AppAuth";
            })
            .AddPolicyScheme("AppAuth", "JWT or Cookie", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        return JwtBearerDefaults.AuthenticationScheme;
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/html/login.html";
                options.Cookie.Name = "HedgehogAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = HedgehogPanel.Core.Config.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = HedgehogPanel.Core.Config.JwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
            logger.Information("Authentication configured with Cookie + JWT (policy scheme).");
        }
        else
        {
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/html/login.html";
                    options.Cookie.Name = "HedgehogAuth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                });
            logger.Warning("JWT secret is not configured or too short; JWT authentication disabled, cookie-only.");
        }
        logger.Information("Authentication configured.");
        
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        logger.Information("Authorization configured.");
        
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-Token";
        });
        logger.Information("Antiforgery configured.");


        logger.Information("Building application...");
        var app = builder.Build();
        logger.Information("Application built.");

        // Initialize static logger with database logger
        var dbLogger = app.Services.GetRequiredService<DatabaseLoggerService>();
        HedgehogLogger.Initialize(dbLogger);
        logger.Information("HedgehogLogger initialized with DatabaseLoggerService.");
        
        app.UseSerilogRequestLogging();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
        }
        // Rate Limiting must be before authentication
        app.UseRateLimiter();
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
                    logger.Warning("Unauthorized access attempt to Admin API by {User}. Authenticated={Authenticated}", username, isAuthenticated);

                    await logger.LogSecurityEventAsync(new SecurityEvent(
                        "Security.UnauthorizedAccessAttempt",
                        null,
                        null,
                        ip,
                        context.Request.Headers["User-Agent"],
                        false,
                        new { username, path = context.Request.Path.ToString() }
                    ));
                }
            }
            await next();
        });

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
        app.UseAuthorization();
        app.UseAntiforgery();

        logger.Information("Mapping redirect for root path...");
        app.MapGet("/", (HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                return Results.File(Path.Combine(env.ContentRootPath, "html", "index.html"), "text/html; charset=utf-8");
            }
            return Results.Redirect("/html/login.html");
        }).AllowAnonymous();
        logger.Information("Redirect mapped.");

        app.MapGet("/error", (HttpContext ctx) =>
        {
            var exception = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
            var detail = app.Environment.IsDevelopment() ? exception?.ToString() : null;
            return Results.Problem(title: "An error occurred while processing your request.", statusCode: 500, detail: detail);
        }).AllowAnonymous();

        logger.Information("Mapping API endpoints...");
        app.MapApi();
        logger.Information("API endpoints mapped.");

        logger.Information("Starting application...");
        app.Run();
    }
}