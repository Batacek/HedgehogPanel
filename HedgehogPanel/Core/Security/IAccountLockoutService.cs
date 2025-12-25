using System;
using System.Threading.Tasks;

namespace HedgehogPanel.Core.Security;

public interface IAccountLockoutService
{
    /// <summary>
    /// Returns true if the account is currently locked.
    /// </summary>
    Task<bool> IsAccountLockedAsync(string username, string clientIp);

    /// <summary>
    /// Records a failed authentication attempt for the account.
    /// </summary>
    Task RecordFailedAttemptAsync(string username, string clientIp);

    /// <summary>
    /// Resets failed attempts info after a successful login.
    /// </summary>
    Task ResetFailedAttemptsAsync(string username, string clientIp);

    /// <summary>
    /// Returns the remaining lockout time if locked; otherwise null.
    /// </summary>
    Task<TimeSpan?> GetLockoutTimeRemainingAsync(string username, string clientIp);
}