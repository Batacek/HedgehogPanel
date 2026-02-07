using Serilog;

namespace HedgehogPanel.Core.Logging;

public class FileLoggerService : ILoggerService
{
    private readonly Serilog.ILogger _logger;

    public FileLoggerService(Type contextType)
    {
        _logger = Log.ForContext(contextType);
    }

    public void Debug(string message, params object[] args) => _logger.Debug(message, args);
    public void Information(string message, params object[] args) => _logger.Information(message, args);
    public void Warning(string message, params object[] args) => _logger.Warning(message, args);
    public void Warning(Exception ex, string message, params object[] args) => _logger.Warning(ex, message, args);
    public void Error(string message, params object[] args) => _logger.Error(message, args);
    public void Error(Exception ex, string message, params object[] args) => _logger.Error(ex, message, args);
    public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);
    public void Fatal(Exception ex, string message, params object[] args) => _logger.Fatal(ex, message, args);

    public Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        _logger.Information("Security Event: {EventType}, Success: {Success}, User: {UserId}, IP: {IpAddress}, Metadata: {@Metadata}",
            securityEvent.EventType, securityEvent.Success, securityEvent.UserId, securityEvent.IpAddress, securityEvent.Metadata);
        return Task.CompletedTask;
    }
}
