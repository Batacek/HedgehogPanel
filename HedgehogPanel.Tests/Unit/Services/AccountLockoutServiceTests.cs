using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using HedgehogPanel.Infrastructure.Security;

namespace HedgehogPanel.Tests.Unit.Services;

public class AccountLockoutServiceTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly AccountLockoutService _service;

    public AccountLockoutServiceTests()
    {
        _mockCache = new Mock<IMemoryCache>();
        _service = new AccountLockoutService(_mockCache.Object, new LockoutSettings());
    }

    [Fact]
    public async Task IsAccountLockedAsync_WhenNoLockoutInfo_ReturnsFalse()
    {
        // Arrange
        object? cacheValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _service.IsAccountLockedAsync("testuser", "127.0.0.1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsAccountLockedAsync_WhenLockedUntilInFuture_ReturnsTrue()
    {
        // Arrange
        var lockoutInfo = CreateLockoutInfo();
        lockoutInfo.GetType().GetProperty("LockedUntil")!.SetValue(lockoutInfo, DateTimeOffset.UtcNow.AddMinutes(5));
        
        object? cacheValue = lockoutInfo;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(true);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _service.IsAccountLockedAsync("testuser", "127.0.0.1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAccountLockedAsync_WhenLockedUntilInPast_ReturnsFalse()
    {
        // Arrange
        var lockoutInfo = CreateLockoutInfo();
        lockoutInfo.GetType().GetProperty("LockedUntil")!.SetValue(lockoutInfo, DateTimeOffset.UtcNow.AddMinutes(-1));
        
        object? cacheValue = lockoutInfo;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(true);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _service.IsAccountLockedAsync("testuser", "127.0.0.1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_FirstAttempt_DoesNotLockAccount()
    {
        // Arrange
        object? cacheValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        var mockEntry = new Mock<ICacheEntry>();
        mockEntry.SetupProperty(e => e.Value);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockEntry.Object);

        // Act
        await _service.RecordFailedAttemptAsync("testuser", "127.0.0.1");

        // Assert
        _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_FifthAttempt_LocksAccount()
    {
        // Arrange
        var lockoutInfo = CreateLockoutInfo();
        var timestamps = lockoutInfo.GetType().GetProperty("FailedTimestamps")!.GetValue(lockoutInfo) as System.Collections.Generic.List<DateTimeOffset>;
        for (int i = 0; i < 4; i++)
        {
            timestamps!.Add(DateTimeOffset.UtcNow.AddMinutes(-i));
        }

        object? cacheValue = lockoutInfo;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(true);
        var mockEntry = new Mock<ICacheEntry>();
        mockEntry.SetupProperty(e => e.Value);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockEntry.Object);

        // Act
        await _service.RecordFailedAttemptAsync("testuser", "127.0.0.1");

        // Assert
        var lockedUntil = lockoutInfo.GetType().GetProperty("LockedUntil")!.GetValue(lockoutInfo) as DateTimeOffset?;
        Assert.NotNull(lockedUntil);
        Assert.True(lockedUntil > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetLockoutTimeRemainingAsync_WhenNotLocked_ReturnsNull()
    {
        // Arrange
        object? cacheValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _service.GetLockoutTimeRemainingAsync("testuser", "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLockoutTimeRemainingAsync_WhenLocked_ReturnsTimeSpan()
    {
        // Arrange
        var lockoutInfo = CreateLockoutInfo();
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(5);
        lockoutInfo.GetType().GetProperty("LockedUntil")!.SetValue(lockoutInfo, futureTime);
        
        object? cacheValue = lockoutInfo;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(true);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _service.GetLockoutTimeRemainingAsync("testuser", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value.TotalMinutes > 4 && result.Value.TotalMinutes <= 5);
    }

    [Fact]
    public async Task ResetFailedAttemptsAsync_RemovesCacheEntry()
    {
        // Arrange & Act
        await _service.ResetFailedAttemptsAsync("testuser", "127.0.0.1");

        // Assert
        _mockCache.Verify(c => c.Remove(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAccountAsync_RemovesBothPerIpAndGlobalEntries()
    {
        // Arrange & Act
        await _service.UnlockAccountAsync("testuser", "127.0.0.1");

        // Assert - admin unlock clears the per-IP counter and the account-global counter.
        _mockCache.Verify(c => c.Remove(It.IsAny<object>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_OldAttemptsOutsideWindow_AreIgnored()
    {
        // Arrange
        var lockoutInfo = CreateLockoutInfo();
        var timestamps = lockoutInfo.GetType().GetProperty("FailedTimestamps")!.GetValue(lockoutInfo) as System.Collections.Generic.List<DateTimeOffset>;
        // Add 4 old attempts (outside 15-minute window)
        for (int i = 0; i < 4; i++)
        {
            timestamps!.Add(DateTimeOffset.UtcNow.AddMinutes(-20 - i));
        }

        object? cacheValue = lockoutInfo;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(true);
        var mockEntry = new Mock<ICacheEntry>();
        mockEntry.SetupProperty(e => e.Value);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockEntry.Object);

        // Act
        await _service.RecordFailedAttemptAsync("testuser", "127.0.0.1");

        // Assert - should not be locked because old attempts are cleaned up
        var lockedUntil = lockoutInfo.GetType().GetProperty("LockedUntil")!.GetValue(lockoutInfo) as DateTimeOffset?;
        Assert.Null(lockedUntil);
    }

    // ----- Account-global throttle (exercised against a real in-memory cache) -----

    private static AccountLockoutService RealService() =>
        new(new MemoryCache(new MemoryCacheOptions()), new LockoutSettings());

    [Fact]
    public async Task PerIp_FourFailures_DoesNotLockThatClient()
    {
        var service = RealService();
        for (var i = 0; i < 4; i++)
        {
            await service.RecordFailedAttemptAsync("targetuser", "10.0.0.1");
        }

        Assert.False(await service.IsAccountLockedAsync("targetuser", "10.0.0.1"));
    }

    [Fact]
    public async Task PerIp_FifthFailure_LocksOnlyThatClient()
    {
        var service = RealService();
        for (var i = 0; i < 5; i++)
        {
            await service.RecordFailedAttemptAsync("targetuser", "10.0.0.1");
        }

        // The offending client is locked, but the account-global ceiling (15) is not yet reached,
        // so a different source IP still gets a fresh budget.
        Assert.True(await service.IsAccountLockedAsync("targetuser", "10.0.0.1"));
        Assert.False(await service.IsAccountLockedAsync("targetuser", "10.0.0.2"));
    }

    [Fact]
    public async Task AccountGlobal_ManyDistinctIps_LockEvenFromAFreshIp()
    {
        var service = RealService();

        // 15 single failures, each from a different IP - none trips the per-IP limit (5),
        // but together they reach the account-global ceiling (15).
        for (var i = 0; i < 15; i++)
        {
            await service.RecordFailedAttemptAsync("targetuser", $"192.0.2.{i}");
        }

        // A source IP that never failed is throttled because the account is globally locked.
        Assert.True(await service.IsAccountLockedAsync("targetuser", "203.0.113.99"));
    }

    [Fact]
    public async Task AccountGlobal_DoesNotAffectOtherAccounts()
    {
        var service = RealService();
        for (var i = 0; i < 15; i++)
        {
            await service.RecordFailedAttemptAsync("targetuser", $"192.0.2.{i}");
        }

        Assert.False(await service.IsAccountLockedAsync("bystander", "203.0.113.99"));
    }

    private object CreateLockoutInfo()
    {
        var type = typeof(AccountLockoutService).GetNestedType("LockoutInfo", System.Reflection.BindingFlags.NonPublic);
        return Activator.CreateInstance(type!)!;
    }
}
