using System;
using System.Threading;

namespace HedgehogPanel.Infrastructure.Security;

/// <summary>
/// Thread-safe, in-memory lockout policy. Editable at runtime by admins; resets to the defaults on
/// restart (persisting it across restarts is tracked together with the rest of the lockout state).
/// </summary>
public sealed class LockoutSettings : ILockoutSettings
{
    public const int DefaultMaxFailedAttempts = 5;
    public const int DefaultLockoutMinutes = 5;

    private const int MinAttempts = 1;
    private const int MaxAttempts = 100;
    private const int MinMinutes = 1;
    private const int MaxMinutes = 1440; // 24 hours

    private int _maxFailedAttempts = DefaultMaxFailedAttempts;
    private int _lockoutMinutes = DefaultLockoutMinutes;

    public int MaxFailedAttempts => Volatile.Read(ref _maxFailedAttempts);

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(Volatile.Read(ref _lockoutMinutes));

    public void Update(int maxFailedAttempts, int lockoutMinutes)
    {
        Volatile.Write(ref _maxFailedAttempts, Math.Clamp(maxFailedAttempts, MinAttempts, MaxAttempts));
        Volatile.Write(ref _lockoutMinutes, Math.Clamp(lockoutMinutes, MinMinutes, MaxMinutes));
    }
}
