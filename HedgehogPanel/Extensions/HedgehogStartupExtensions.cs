using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Serilog;
using HedgehogPanel.API;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Database;
using HedgehogPanel.Core.Managers;
using HedgehogPanel.Core.Configuration;
using HedgehogPanel.Core.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace HedgehogPanel.Extensions;

public static class HedgehogStartupExtensions
{
    public static WebApplicationBuilder AddHedgehogServices(this WebApplicationBuilder builder, string[] args)
    {
        try
        {
            Env.Load();
        }
        catch { /* ignore */ }

        var config = ConfigLoader.Load(args);
        builder.Services.AddSingleton(config);

        InitializeSerilog(config);
        
        var logger = HedgehogLogger.ForContext<WebApplicationBuilder>();
        logger.Information("Starting Hedgehog Panel Web Application...");
        logger.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        logger.Information("Content Root Path: {ContentRootPath}", builder.Environment.ContentRootPath);
        logger.Information("Web Root Path: {WebRootPath}", Path.Combine(builder.Environment.ContentRootPath, "Web", "wwwroot"));

        builder.WebHost.ConfigureKestrel(options =>
        {
            if (System.Net.IPAddress.TryParse(config.Server.ListenAddress, out var addr))
                options.Listen(addr, config.Server.Port);
            else
                options.ListenAnyIP(config.Server.Port);
        });
        logger.Information("Builder initialized. Listening on {Address}:{Port}", config.Server.ListenAddress, config.Server.Port);
        
        builder.Host.UseSerilog();

        if (config.Security.Cors.Enabled)
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(config.Security.Cors.AllowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                    if (!config.Security.Cors.AllowedOrigins.Contains("*"))
                    {
                        policy.AllowCredentials();
                    }
                });
            });
        }

        logger.Information("Configuring services...");

        // Database configuration
        if (string.IsNullOrEmpty(config.Database.Username)) throw new InvalidOperationException("DB_USER (Database.Username) must be set.");
        if (string.IsNullOrEmpty(config.Database.Password)) throw new InvalidOperationException("DB_PASSWORD (Database.Password) must be set.");
        if (string.IsNullOrEmpty(config.Database.Name)) throw new InvalidOperationException("DB_NAME (Database.Name) must be set.");
        
        var connectionString = config.Database.ConnectionString;

        builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
        builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        builder.Services.AddSingleton<DatabaseLoggerService>();
        builder.Services.AddSingleton<ILoggerService>(sp => sp.GetRequiredService<DatabaseLoggerService>());

        // Note: This is an anti-pattern as it builds a temporary service provider
        // but we're keeping it for now as it was in the original Program.cs
        HedgehogLogger.Initialize(builder.Services.BuildServiceProvider().GetRequiredService<DatabaseLoggerService>());

        builder.Services.AddSingleton<IInMemoryStore, InMemoryStore>();
        builder.Services.AddSingleton<IDataProvider, DataProvider>();
        
        // Logical layers registration
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
        builder.Services.AddSingleton<IServerRepository, ServerRepository>();
        builder.Services.AddSingleton<IAccountService, AccountService>();
        builder.Services.AddSingleton<IServerService, ServerService>();

        builder.Services.AddSingleton<IAccountManager, AccountManager>(sp => 
            new AccountManager(HedgehogLogger.ForContext<AccountManager>(), sp.GetRequiredService<IDbConnectionFactory>()));
        builder.Services.AddSingleton<IServerManager, ServerManager>(sp => 
            new ServerManager(HedgehogLogger.ForContext<ServerManager>(), sp.GetRequiredService<IDbConnectionFactory>()));
        
        logger.Information("Database services and managers registered.");

        // Rate Limiting configuration
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var hedgehogLogger = HedgehogLogger.ForContext(typeof(HedgehogStartupExtensions));
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

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter($"global:{ip}", _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = config.Security.RateLimit.Enabled ? config.Security.RateLimit.RequestsPerMinute : 1000,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.AddPolicy("LoginRateLimit", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter($"login:{ip}", _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<HedgehogPanel.Core.Security.IAccountLockoutService, HedgehogPanel.Core.Security.AccountLockoutService>();

        logger.Information("Setting up authentication and authorization...");
        var jwtSecret = config.Auth.Jwt.Secret;
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
                options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = config.Auth.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = config.Auth.Jwt.Audience,
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
                    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                });
            logger.Warning("JWT secret is not configured or too short; JWT authentication disabled, cookie-only.");
        }

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-Token";
        });

        return builder;
    }

    public static WebApplication UseHedgehogMiddleware(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<HedgehogConfig>();
        var logger = HedgehogLogger.ForContext<WebApplication>();

        // Initialize static logger with database logger
        var dbLogger = app.Services.GetRequiredService<DatabaseLoggerService>();
        HedgehogLogger.Initialize(dbLogger);
        logger.Information("HedgehogLogger initialized with DatabaseLoggerService.");

        // Content Security Policy (CSP) and other security headers
        app.Use(async (context, next) =>
        {
            // Generate a unique nonce for this request
            var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
            context.Items["csp-nonce"] = nonce;
            
            context.Response.Headers.Append("Content-Security-Policy", 
                "default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self';");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        app.UseSerilogRequestLogging();
        if (config.Security.Cors.Enabled)
        {
            app.UseCors();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errApp =>
            {
                errApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    var problem = new
                    {
                        title = "An error occurred while processing your request.",
                        status = 500,
                        traceId = context.TraceIdentifier
                    };
                    await context.Response.WriteAsJsonAsync(problem);
                });
            });
        }

        app.UseRateLimiter();
        app.UseAuthentication();

        // Unauthorized access logging middleware
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
                var isAdmin = isAuthenticated && (context.User.IsInRole("Admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"));
                if (!isAdmin)
                {
                    var username = context.User?.FindFirst("username")?.Value ?? context.User?.Identity?.Name ?? "Anonymous";
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    logger.Warning("Unauthorized access attempt to Admin API by {User}. Authenticated={Authenticated}", username, isAuthenticated);

                    var hedgehogLogger = HedgehogLogger.ForContext<WebApplication>();
                    await hedgehogLogger.LogSecurityEventAsync(new SecurityEvent(
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

        // Page protection middleware
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
                    if (isLoginPage)
                    {
                        context.Response.Redirect("/");
                        return;
                    }

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

        app.UseAntiforgery();

        // Antiforgery validation middleware
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var method = context.Request.Method;
            var isApi = path.StartsWithSegments("/api");
            var isStateChanging = method == "POST" || method == "PUT" || method == "PATCH" || method == "DELETE";

            if (isApi && isStateChanging)
            {
                var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Antiforgery token validation failed." });
                    return;
                }
            }
            await next();
        });

        // Antiforgery token distribution middleware
        app.Use(async (context, next) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, 
                new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = !app.Environment.IsDevelopment() });
            await next();
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Web", "wwwroot")),
            RequestPath = "/html"
        });

        app.UseAuthorization();

        // Endpoints
        app.MapGet("/", async (HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                var indexPath = Path.Combine(env.ContentRootPath, "Web", "wwwroot", "index.html");
                var html = await File.ReadAllTextAsync(indexPath);
                var nonce = ctx.Items["csp-nonce"]?.ToString() ?? "";
                
                // Inject nonce into the main script tag and add data attribute for JS access
                html = html.Replace("<script src=\"/html/js/main.js\"></script>", 
                    $"<script src=\"/html/js/main.js\" nonce=\"{nonce}\" data-csp-nonce=\"{nonce}\"></script>");
                
                return Results.Content(html, "text/html; charset=utf-8");
            }
            return Results.Redirect("/html/login.html");
        }).AllowAnonymous();

        app.MapGet("/error", (HttpContext ctx) =>
        {
            var exception = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (app.Environment.IsDevelopment())
            {
                return Results.Problem(
                    title: "An error occurred while processing your request.",
                    detail: exception?.ToString(),
                    statusCode: 500);
            }
            return Results.Problem(
                title: "An error occurred while processing your request.",
                statusCode: 500);
        }).AllowAnonymous();

        app.MapGet("/html/login.html", async (HttpContext ctx, IWebHostEnvironment env) =>
        {
            var loginPath = Path.Combine(env.ContentRootPath, "Web", "wwwroot", "login.html");
            var html = await File.ReadAllTextAsync(loginPath);
            var nonce = ctx.Items["csp-nonce"]?.ToString() ?? "";
            
            // Inject nonce into the inline script tag
            html = html.Replace("<script>", $"<script nonce=\"{nonce}\">");
            
            return Results.Content(html, "text/html; charset=utf-8");
        }).AllowAnonymous();

        app.MapApi();

        return app;
    }

    private static void InitializeSerilog(HedgehogConfig config)
    {
        try
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(Enum.TryParse<Serilog.Events.LogEventLevel>(config.Logging.Level, true, out var level) ? level : Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            if (config.Logging.LogToConsole)
            {
                loggerConfig.WriteTo.Console(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
                );
            }

            if (config.Logging.LogToFile)
            {
                loggerConfig.WriteTo.File(
                    config.Logging.File.Path.Replace("{Date}", ""),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: config.Logging.File.MaxFiles,
                    fileSizeLimitBytes: config.Logging.File.MaxSizeMB * 1024 * 1024,
                    shared: true,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
                );
            }

            Log.Logger = loggerConfig.CreateLogger();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize logger: {ex}");
        }
    }
}
