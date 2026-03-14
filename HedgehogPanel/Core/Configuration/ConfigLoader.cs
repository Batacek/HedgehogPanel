using System;
using Microsoft.Extensions.Configuration;

namespace HedgehogPanel.Core.Configuration;

public static class ConfigLoader
{
    public static HedgehogConfig Load(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var config = new HedgehogConfig();
        configuration.Bind(config);

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        if (!string.IsNullOrEmpty(dbHost)) config.Database.Host = dbHost;
        
        var dbPortStr = Environment.GetEnvironmentVariable("DB_PORT");
        if (int.TryParse(dbPortStr, out var dbPort)) config.Database.Port = dbPort;
        
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        if (!string.IsNullOrEmpty(dbUser)) config.Database.Username = dbUser;
        
        var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrEmpty(dbPass)) config.Database.Password = dbPass;
        
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        if (!string.IsNullOrEmpty(dbName)) config.Database.Name = dbName;
        
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(jwtSecret)) config.Auth.Jwt.Secret = jwtSecret;

        return config;
    }
}
