namespace HedgehogPanel.Core.Logging;

/// <summary>
/// Defines a contract for logging services, supporting multiple log levels and the ability to log security events.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs a debug-level message with optional parameters.
    /// This method is used for detailed diagnostic output that can
    /// be helpful during development or troubleshooting.
    /// </summary>
    /// <param name="message">The debug message template to log. Use structured logging
    /// to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message.</param>
    void Debug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message with optional parameters.
    /// This method is used to record general information that can be helpful
    /// for understanding application flow or state.
    /// </summary>
    /// <param name="message">The information message template to log. Use structured logging
    /// to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message.</param>
    void Information(string message, params object[] args);

    /// <summary>
    /// Logs a warning-level message with optional parameters.
    /// This method is used to highlight events that might indicate
    /// potential issues or situations that should be monitored.
    /// </summary>
    /// <param name="message">The warning message template to log. Use structured logging
    /// to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message.</param>
    void Warning(string message, params object[] args);

    /// <summary>
    /// Logs a warning-level message with an associated exception and optional parameters.
    /// This method is used to indicate potential issues or important situations that
    /// require attention but may not be immediately critical.
    /// </summary>
    /// <param name="ex">The exception associated with the warning. This can provide additional
    /// context or detail about the potential issue.</param>
    /// <param name="message">The warning message template to log. Use structured logging
    /// to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message.</param>
    void Warning(Exception ex, string message, params object[] args);

    /// <summary>
    /// Logs an error-level message with optional parameters.
    /// This method is used to capture significant issues or failures
    /// that may require immediate attention or investigation.
    /// </summary>
    /// <param name="message">The error message template to log. Use structured logging
    /// to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message.</param>
    void Error(string message, params object[] args);

    /// <summary>
    /// Logs an error-level message along with an exception and optional parameters.
    /// This method is typically used to capture and log critical issues and exceptions
    /// that require immediate attention or detailed analysis.
    /// </summary>
    /// <param name="ex">The exception associated with the error being logged. Provides detailed information
    /// about the error, including stack trace and message.</param>
    /// <param name="message">The error message template to log. Use structured logging to include placeholders within the message.</param>
    /// <param name="args">Optional parameters to populate placeholders in the message. These provide additional context to the log entry.</param>
    void Error(Exception ex, string message, params object[] args);

    /// <summary>
    /// Logs a security-related event asynchronously.
    /// This method captures details of actions or incidents,
    /// aiding in compliance, auditing, and security analysis.
    /// </summary>
    /// <param name="securityEvent">The security event to log, containing event type, user/actor details,
    /// IP address, user agent, success status, and optionally additional metadata.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
}
