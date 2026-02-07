namespace HedgehogPanel.Core.Logging;

public class HedgehogLogger : ILoggerService
{
    private readonly FileLoggerService _fileLogger;
    private readonly DatabaseLoggerService _dbLogger;

    public HedgehogLogger(Type contextType)
    {
        _fileLogger = new FileLoggerService(contextType);
        _dbLogger = new DatabaseLoggerService();
    }

    public void Debug(string message, params object[] args) => _fileLogger.Debug(message, args);
    public void Information(string message, params object[] args) => _fileLogger.Information(message, args);
    public void Warning(string message, params object[] args) => _fileLogger.Warning(message, args);
    public void Warning(Exception ex, string message, params object[] args) => _fileLogger.Warning(ex, message, args);
    public void Error(string message, params object[] args) => _fileLogger.Error(message, args);
    public void Error(Exception ex, string message, params object[] args) => _fileLogger.Error(ex, message, args);

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        await _fileLogger.LogSecurityEventAsync(securityEvent);

        await _dbLogger.LogSecurityEventAsync(securityEvent);
    }

    public static ILoggerService ForContext<T>() => new HedgehogLogger(typeof(T));
    public static ILoggerService ForContext(Type type) => new HedgehogLogger(type);
}
