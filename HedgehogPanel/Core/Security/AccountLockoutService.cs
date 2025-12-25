using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using ILogger = Serilog.ILogger;

namespace HedgehogPanel.Core.Security;

public class AccountLockoutService : IAccountLockoutService
{
    private static readonly ILogger Logger = Log.ForContext<AccountLockoutService>();

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan FailedAttemptsWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public AccountLockoutService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string Key(string username, string clientIp) => $"lockout:{username?.ToLowerInvariant()}|{clientIp}";

    private class LockoutInfo
    {
        public List<DateTimeOffset> FailedTimestamps { get; } = new();
        public DateTimeOffset? LockedUntil { get; set; }
    }

    public Task<bool> IsAccountLockedAsync(string username, string clientIp)
    {
        var info = GetInfo(username, clientIp);
        CleanupWindow(info);
        var locked = info.LockedUntil != null && info.LockedUntil > DateTimeOffset.UtcNow;
        if (locked)
        {
            var remaining = info.LockedUntil!.Value - DateTimeOffset.UtcNow;
            Logger.Information("Account {Username} from {IP} is locked. Remaining {Remaining}.", username, clientIp, remaining);
        }
        return Task.FromResult(locked);
    }

    public Task RecordFailedAttemptAsync(string username, string clientIp)
    {
        var info = GetInfo(username, clientIp);
        CleanupWindow(info);
        info.FailedTimestamps.Add(DateTimeOffset.UtcNow);
        Logger.Warning("Failed login attempt for {Username} from {IP}. Count (last {Window}): {Count}.", username, clientIp, FailedAttemptsWindow, info.FailedTimestamps.Count);
        if (info.FailedTimestamps.Count >= MaxFailedAttempts)
        {
            info.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
            Logger.Warning("Account {Username} from {IP} locked until {LockedUntil} after {Count} failed attempts.", username, clientIp, info.LockedUntil, info.FailedTimestamps.Count);
        }
        SetInfo(username, clientIp, info);
        return Task.CompletedTask;
    }

    public Task ResetFailedAttemptsAsync(string username, string clientIp)
    {
        var key = Key(username, clientIp);
        _cache.Remove(key);
        Logger.Information("Reset lockout counters for {Username} from {IP} after successful login.", username, clientIp);
        return Task.CompletedTask;
    }

    public Task<TimeSpan?> GetLockoutTimeRemainingAsync(string username, string clientIp)
    {
        var info = GetInfo(username, clientIp);
        CleanupWindow(info);
        if (info.LockedUntil is { } until && until > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<TimeSpan?>(until - DateTimeOffset.UtcNow);
        }
        return Task.FromResult<TimeSpan?>(null);
    }

    private LockoutInfo GetInfo(string username, string clientIp)
    {
        var key = Key(username, clientIp);
        if (!_cache.TryGetValue(key, out LockoutInfo? info) || info is null)
        {
            info = new LockoutInfo();
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(6));
            _cache.Set(key, info, options);
        }
        return info;
    }

    private void SetInfo(string username, string clientIp, LockoutInfo info)
    {
        var key = Key(username, clientIp);
        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(6));
        _cache.Set(key, info, options);
    }

    private static void CleanupWindow(LockoutInfo info)
    {
        var cutoff = DateTimeOffset.UtcNow - FailedAttemptsWindow;
        if (info.FailedTimestamps.Count == 0 && info.LockedUntil == null)
            return;
        var before = info.FailedTimestamps.Count;
        info.FailedTimestamps.RemoveAll(ts => ts < cutoff);
        if (before != info.FailedTimestamps.Count)
        {
            Logger.Debug("Cleaned up failed attempts window. Before={Before}, After={After}", before, info.FailedTimestamps.Count);
        }
        // Unlock automatically if time passed
        if (info.LockedUntil != null && info.LockedUntil <= DateTimeOffset.UtcNow)
        {
            info.LockedUntil = null;
            Logger.Information("Auto-unlocked account after lockout window elapsed.");
        }
    }
}