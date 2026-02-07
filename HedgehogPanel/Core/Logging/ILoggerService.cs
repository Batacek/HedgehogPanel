using System;
using System.Threading.Tasks;

namespace HedgehogPanel.Core.Logging;

public interface ILoggerService
{
    void Debug(string message, params object[] args);
    void Information(string message, params object[] args);
    void Warning(string message, params object[] args);
    void Warning(Exception ex, string message, params object[] args);
    void Error(string message, params object[] args);
    void Error(Exception ex, string message, params object[] args);

    Task LogSecurityEventAsync(SecurityEvent securityEvent);
}
