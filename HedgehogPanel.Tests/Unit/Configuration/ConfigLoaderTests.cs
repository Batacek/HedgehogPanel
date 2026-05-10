using Xunit;
using System;
using HedgehogPanel.Infrastructure.Configuration;

namespace HedgehogPanel.Tests.Unit.Configuration;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_WithEmptyArgs_ReturnsDefaultConfig()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var config = ConfigLoader.Load(args);

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Database);
        Assert.NotNull(config.Auth);
        Assert.NotNull(config.Server);
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesDbHost()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalHost = Environment.GetEnvironmentVariable("DB_HOST");
        try
        {
            Environment.SetEnvironmentVariable("DB_HOST", "testhost.example.com");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("testhost.example.com", config.Database.Host);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_HOST", originalHost);
        }
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesDbPort()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalPort = Environment.GetEnvironmentVariable("DB_PORT");
        try
        {
            Environment.SetEnvironmentVariable("DB_PORT", "5433");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal(5433, config.Database.Port);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_PORT", originalPort);
        }
    }

    [Fact]
    public void Load_WithInvalidDbPort_KeepsDefaultPort()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalPort = Environment.GetEnvironmentVariable("DB_PORT");
        var originalDbPort = 0;
        try
        {
            // Get default port first
            var defaultConfig = ConfigLoader.Load(Array.Empty<string>());
            originalDbPort = defaultConfig.Database.Port;

            Environment.SetEnvironmentVariable("DB_PORT", "invalid");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal(originalDbPort, config.Database.Port);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_PORT", originalPort);
        }
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesDbUser()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalUser = Environment.GetEnvironmentVariable("DB_USER");
        try
        {
            Environment.SetEnvironmentVariable("DB_USER", "testuser");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("testuser", config.Database.Username);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_USER", originalUser);
        }
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesDbPassword()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass123");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("testpass123", config.Database.Password);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_PASSWORD", originalPassword);
        }
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesDbName()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalName = Environment.GetEnvironmentVariable("DB_NAME");
        try
        {
            Environment.SetEnvironmentVariable("DB_NAME", "testdb");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("testdb", config.Database.Name);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_NAME", originalName);
        }
    }

    [Fact]
    public void Load_WithEnvironmentVariables_OverridesJwtSecret()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        try
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key-12345");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("test-secret-key-12345", config.Auth.Jwt.Secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", originalSecret);
        }
    }

    [Fact]
    public void Load_WithMultipleEnvironmentVariables_OverridesAll()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalHost = Environment.GetEnvironmentVariable("DB_HOST");
        var originalUser = Environment.GetEnvironmentVariable("DB_USER");
        var originalPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("DB_HOST", "multi.example.com");
            Environment.SetEnvironmentVariable("DB_USER", "multiuser");
            Environment.SetEnvironmentVariable("DB_PASSWORD", "multipass");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal("multi.example.com", config.Database.Host);
            Assert.Equal("multiuser", config.Database.Username);
            Assert.Equal("multipass", config.Database.Password);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_HOST", originalHost);
            Environment.SetEnvironmentVariable("DB_USER", originalUser);
            Environment.SetEnvironmentVariable("DB_PASSWORD", originalPassword);
        }
    }

    [Fact]
    public void Load_WithEmptyEnvironmentVariable_DoesNotOverride()
    {
        // Arrange
        var args = Array.Empty<string>();
        var originalHost = Environment.GetEnvironmentVariable("DB_HOST");
        try
        {
            // Get default host first
            var defaultConfig = ConfigLoader.Load(Array.Empty<string>());
            var defaultHost = defaultConfig.Database.Host;

            Environment.SetEnvironmentVariable("DB_HOST", "");

            // Act
            var config = ConfigLoader.Load(args);

            // Assert
            Assert.Equal(defaultHost, config.Database.Host);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_HOST", originalHost);
        }
    }

    [Fact]
    public void HedgehogConfig_DatabaseConnectionString_IsFormatted()
    {
        // Arrange
        var config = new HedgehogConfig
        {
            Database = new DatabaseConfig
            {
                Host = "testhost",
                Port = 5432,
                Username = "testuser",
                Password = "testpass",
                Name = "testdb"
            }
        };

        // Act
        var connectionString = config.Database.ConnectionString;

        // Assert
        Assert.Contains("Host=testhost", connectionString);
        Assert.Contains("Port=5432", connectionString);
        Assert.Contains("Username=testuser", connectionString);
        Assert.Contains("Password=testpass", connectionString);
        Assert.Contains("Database=testdb", connectionString);
    }
}
