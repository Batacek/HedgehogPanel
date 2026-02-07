namespace HedgehogPanel.Core.Logging;

public class HedgehogLogger : ILoggerService
{
    private readonly FileLoggerService _fileLogger;
    private readonly DatabaseLoggerService _dbLogger;

    private static DatabaseLoggerService? _dbLoggerInstance;

    public HedgehogLogger(Type contextType, DatabaseLoggerService? dbLogger = null)
    {
        _fileLogger = new FileLoggerService(contextType);
        _dbLogger = dbLogger;
    }

    public static void Initialize(DatabaseLoggerService dbLogger)
    {
        _dbLoggerInstance = dbLogger;
    }

    public void Debug(string message, params object[] args) => _fileLogger.Debug(message, args);
    public void Information(string message, params object[] args) => _fileLogger.Information(message, args);
    public void Warning(string message, params object[] args) => _fileLogger.Warning(message, args);
    public void Warning(Exception ex, string message, params object[] args) => _fileLogger.Warning(ex, message, args);
    public void Error(string message, params object[] args) => _fileLogger.Error(message, args);
    public void Error(Exception ex, string message, params object[] args) => _fileLogger.Error(ex, message, args);
    public void Fatal(string message, params object[] args) => _fileLogger.Fatal(message, args);
    public void Fatal(Exception ex, string message, params object[] args) => _fileLogger.Fatal(ex, message, args);

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        await _fileLogger.LogSecurityEventAsync(securityEvent);

        if (_dbLogger != null)
        {
            await _dbLogger.LogSecurityEventAsync(securityEvent);
        }
        else if (_dbLoggerInstance != null)
        {
            await _dbLoggerInstance.LogSecurityEventAsync(securityEvent);
        }
    }

    public static ILoggerService ForContext<T>() => new HedgehogLogger(typeof(T), _dbLoggerInstance);
    public static ILoggerService ForContext(Type type) => new HedgehogLogger(type, _dbLoggerInstance);
}
