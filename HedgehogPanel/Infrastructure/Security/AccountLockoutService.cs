using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Infrastructure.Logging;

namespace HedgehogPanel.Infrastructure.Security;

public class AccountLockoutService : IAccountLockoutService
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(AccountLockoutService));

    private static readonly TimeSpan FailedAttemptsWindow = TimeSpan.FromMinutes(15);
    private const int MaxGlobalFailedAttempts = 15;
    private static readonly TimeSpan GlobalLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;
    private readonly ILockoutSettings _settings;

    public AccountLockoutService(IMemoryCache cache, ILockoutSettings settings)
    {
        _cache = cache;
        _settings = settings;
    }

    private static string PerIpKey(string username, string clientIp) => $"lockout:{username?.ToLowerInvariant()}|{clientIp}";
    private static string GlobalKey(string username) => $"lockout-global:{username?.ToLowerInvariant()}";

    private class LockoutInfo
    {
        public List<DateTimeOffset> FailedTimestamps { get; } = new();
        public DateTimeOffset? LockedUntil { get; set; }
    }

    public Task<bool> IsAccountLockedAsync(string username, string clientIp)
    {
        var lockedUntil = MaxRemainingLockout(username, clientIp);
        var locked = lockedUntil != null && lockedUntil > DateTimeOffset.UtcNow;
        if (locked)
        {
            Logger.Information("Account {Username} from {IP} is locked. Remaining {Remaining}.", username, clientIp, lockedUntil!.Value - DateTimeOffset.UtcNow);
        }
        return Task.FromResult(locked);
    }

    public Task RecordFailedAttemptAsync(string username, string clientIp)
    {
        RecordInBucket(PerIpKey(username, clientIp), _settings.MaxFailedAttempts, _settings.LockoutDuration, username, clientIp, isGlobal: false);
        RecordInBucket(GlobalKey(username), MaxGlobalFailedAttempts, GlobalLockoutDuration, username, clientIp, isGlobal: true);
        return Task.CompletedTask;
    }

    public Task ResetFailedAttemptsAsync(string username, string clientIp)
    {
        _cache.Remove(PerIpKey(username, clientIp));
        Logger.Information("Reset lockout counters for {Username} from {IP} after successful login.", username, clientIp);
        return Task.CompletedTask;
    }

    public Task<TimeSpan?> GetLockoutTimeRemainingAsync(string username, string clientIp)
    {
        var until = MaxRemainingLockout(username, clientIp);
        if (until is { } value && value > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<TimeSpan?>(value - DateTimeOffset.UtcNow);
        }
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task UnlockAccountAsync(string username, string clientIp)
    {
        _cache.Remove(PerIpKey(username, clientIp));
        _cache.Remove(GlobalKey(username));
        Logger.Information("Manually unlocked account {Username} from {IP} by admin.", username, clientIp);
        return Task.CompletedTask;
    }

    /// <summary>Returns the later of the per-IP and account-global lockout expiries, or null if neither is locked.</summary>
    private DateTimeOffset? MaxRemainingLockout(string username, string clientIp)
    {
        var perIp = LockedUntil(PerIpKey(username, clientIp));
        var global = LockedUntil(GlobalKey(username));
        if (perIp == null) return global;
        if (global == null) return perIp;
        return perIp > global ? perIp : global;
    }

    private DateTimeOffset? LockedUntil(string key)
    {
        var info = GetInfo(key);
        CleanupWindow(info);
        if (info.LockedUntil is { } until && until > DateTimeOffset.UtcNow)
        {
            return until;
        }
        return null;
    }

    private void RecordInBucket(string key, int threshold, TimeSpan lockoutDuration, string username, string clientIp, bool isGlobal)
    {
        var info = GetInfo(key);
        CleanupWindow(info);
        info.FailedTimestamps.Add(DateTimeOffset.UtcNow);
        var scope = isGlobal ? "account-global" : "per-IP";
        Logger.Warning("Failed login attempt for {Username} from {IP} ({Scope}). Count (last {Window}): {Count}.", username, clientIp, scope, FailedAttemptsWindow, info.FailedTimestamps.Count);

        if (info.FailedTimestamps.Count >= threshold && info.LockedUntil == null)
        {
            info.LockedUntil = DateTimeOffset.UtcNow.Add(lockoutDuration);
            Logger.Warning("Account {Username} locked until {LockedUntil} after {Count} failed attempts ({Scope}).", username, info.LockedUntil, info.FailedTimestamps.Count, scope);

            _ = Logger.LogSecurityEventAsync(new SecurityEvent(
                "Security.AccountLockout",
                null,
                null,
                clientIp,
                null,
                true,
                new { username, threshold, scope, signal = "Too many failed attempts" }
            ));
        }
        SetInfo(key, info);
    }

    private LockoutInfo GetInfo(string key)
    {
        if (!_cache.TryGetValue(key, out LockoutInfo? info) || info is null)
        {
            info = new LockoutInfo();
            SetInfo(key, info);
        }
        return info;
    }

    private void SetInfo(string key, LockoutInfo info)
    {
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