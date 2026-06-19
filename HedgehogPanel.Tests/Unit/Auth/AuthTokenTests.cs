using Xunit;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Reflection;
using HedgehogPanel.API.Auth;
using HedgehogPanel.Infrastructure.Configuration;

namespace HedgehogPanel.Tests.Unit.Auth;

public class AuthTokenTests
{
    [Fact]
    public void GenerateJwtToken_WithValidConfig_ReturnsToken()
    {
        // Arrange
        var config = CreateValidConfig();
        var claims = new List<Claim>
        {
            new Claim("username", "testuser"),
            new Claim("guid", Guid.NewGuid().ToString())
        };

        // Act
        var token = InvokeGenerateJwtToken(claims, config);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT has dots separating parts
    }

    [Fact]
    public void GenerateJwtToken_WithNullSecret_ThrowsException()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Auth.Jwt.Secret = null;
        var claims = new List<Claim> { new Claim("username", "testuser") };

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeGenerateJwtToken(claims, config));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("JWT secret not configured", exception.InnerException.Message);
    }

    [Fact]
    public void GenerateJwtToken_WithEmptySecret_ThrowsException()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Auth.Jwt.Secret = "";
        var claims = new List<Claim> { new Claim("username", "testuser") };

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeGenerateJwtToken(claims, config));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("JWT secret not configured", exception.InnerException.Message);
    }

    [Fact]
    public void GenerateJwtToken_WithShortSecret_ThrowsException()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Auth.Jwt.Secret = "tooshort"; // Less than 32 bytes
        var claims = new List<Claim> { new Claim("username", "testuser") };

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeGenerateJwtToken(claims, config));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("JWT secret too short", exception.InnerException.Message);
    }

    [Fact]
    public void VerifyToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var config = CreateValidConfig();
        var claims = new List<Claim>
        {
            new Claim("username", "testuser"),
            new Claim("guid", Guid.NewGuid().ToString())
        };
        var token = InvokeGenerateJwtToken(claims, config);

        // Act
        var result = AuthEndpoints.VerifyToken(token, config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        var invalidToken = "invalid.token.here";

        // Act
        var result = AuthEndpoints.VerifyToken(invalidToken, config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithNullSecret_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Auth.Jwt.Secret = null;
        var token = "some.token.value";

        // Act
        var result = AuthEndpoints.VerifyToken(token, config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithShortSecret_ReturnsFalse()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Auth.Jwt.Secret = "short";
        var token = "some.token.value";

        // Act
        var result = AuthEndpoints.VerifyToken(token, config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithWrongSecret_ReturnsFalse()
    {
        // Arrange
        var config1 = CreateValidConfig();
        var config2 = CreateValidConfig();
        config2.Auth.Jwt.Secret = "different-secret-key-that-is-long-enough-32bytes!";
        
        var claims = new List<Claim> { new Claim("username", "testuser") };
        var token = InvokeGenerateJwtToken(claims, config1);

        // Act
        var result = AuthEndpoints.VerifyToken(token, config2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FormatTime_WithPositiveTimeSpan_ReturnsFormattedString()
    {
        // Arrange
        var span = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30));

        // Act
        var result = InvokeFormatTime(span);

        // Assert
        Assert.Equal("05:30", result);
    }

    [Fact]
    public void FormatTime_WithZeroTimeSpan_ReturnsZeroString()
    {
        // Arrange
        var span = TimeSpan.Zero;

        // Act
        var result = InvokeFormatTime(span);

        // Assert
        Assert.Equal("00:00", result);
    }

    [Fact]
    public void FormatTime_WithNegativeTimeSpan_ReturnsZeroString()
    {
        // Arrange
        var span = TimeSpan.FromMinutes(-5);

        // Act
        var result = InvokeFormatTime(span);

        // Assert
        Assert.Equal("00:00", result);
    }

    [Fact]
    public void FormatTime_WithLargeTimeSpan_ReturnsFormattedString()
    {
        // Arrange
        var span = TimeSpan.FromMinutes(125).Add(TimeSpan.FromSeconds(45));

        // Act
        var result = InvokeFormatTime(span);

        // Assert
        Assert.Equal("125:45", result);
    }

    [Fact]
    public void FormatTime_WithOnlySeconds_ReturnsFormattedString()
    {
        // Arrange
        var span = TimeSpan.FromSeconds(45);

        // Act
        var result = InvokeFormatTime(span);

        // Assert
        Assert.Equal("00:45", result);
    }

    private HedgehogConfig CreateValidConfig()
    {
        return new HedgehogConfig
        {
            Auth = new AuthConfig
            {
                Jwt = new JwtConfig
                {
                    Secret = "this-is-a-very-long-secret-key-for-jwt-token-generation-32bytes",
                    Issuer = "HedgehogPanel",
                    Audience = "HedgehogPanel.Client",
                    ExpiresInMinutes = 60
                }
            }
        };
    }

    private string InvokeGenerateJwtToken(List<Claim> claims, HedgehogConfig config)
    {
        var method = typeof(AuthEndpoints).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method!.Invoke(null, new object[] { claims, config })!;
    }

    private string InvokeFormatTime(TimeSpan span)
    {
        var method = typeof(AuthEndpoints).GetMethod("FormatTime", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method!.Invoke(null, new object[] { span })!;
    }
}
