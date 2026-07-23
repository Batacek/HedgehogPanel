using System;

namespace HedgehogPanel.Infrastructure.Security;

/// <summary>
/// Runtime-adjustable account-lockout policy: how many failed attempts trip the per-IP lockout and
/// how long that lockout lasts. Admins change these values through the panel settings.
/// </summary>
public interface ILockoutSettings
{
    /// <summary>Number of failed attempts from a single IP that triggers a lockout.</summary>
    int MaxFailedAttempts { get; }

    /// <summary>How long a per-IP lockout lasts.</summary>
    TimeSpan LockoutDuration { get; }

    /// <summary>Applies new values (implementations clamp them to a safe range).</summary>
    void Update(int maxFailedAttempts, int lockoutMinutes);
}
