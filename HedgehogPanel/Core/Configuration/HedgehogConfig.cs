namespace HedgehogPanel.Core.Configuration;

public class HedgehogConfig
{
    public ServerConfig Server { get; set; } = new();
    public AuthConfig Auth { get; set; } = new();
    public DaemonConfig Daemon { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
}

public class ServerConfig
{
    public string ListenAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 5050;
}

public class AuthConfig
{
    public JwtConfig Jwt { get; set; } = new();
}

public class JwtConfig
{
    public string Issuer { get; set; } = "HedgehogPanel";
    public string Audience { get; set; } = "HedgehogPanel.Client";
    public int ExpiresInMinutes { get; set; } = 60;
    public int RefreshTokenExpiresInDays { get; set; } = 30;
    public string? Secret { get; set; }
}

public class DaemonConfig
{
    public string ApiVersion { get; set; } = "v1";
    public int RequestTimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 3;
    public bool AllowInsecureConnections { get; set; } = false;
}

public class SecurityConfig
{
    public string AllowedHosts { get; set; } = "*";
    public RateLimitConfig RateLimit { get; set; } = new();
    public CorsConfig Cors { get; set; } = new();
}

public class RateLimitConfig
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 120;
}

public class CorsConfig
{
    public bool Enabled { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = ["*"];
}

public class FeaturesConfig
{
    public bool RegistrationEnabled { get; set; } = true;
    public bool EmailVerification { get; set; } = false;
    public bool ApiKeys { get; set; } = false;
    public bool AuditLogging { get; set; } = true;
}

public class LoggingConfig
{
    public string Level { get; set; } = "Information";
    public bool LogToConsole { get; set; } = true;
    public bool LogToFile { get; set; } = true;
    public FileLoggingConfig File { get; set; } = new();
}

public class FileLoggingConfig
{
    public string Path { get; set; } = "logs/hedgehog-panel-{Date}.log";
    public string DateFormat { get; set; } = "yyyyMMdd";
    public int MaxSizeMB { get; set; } = 10;
    public int MaxFiles { get; set; } = 5;
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Name { get; set; }

    public string ConnectionString => $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Name}";
}
