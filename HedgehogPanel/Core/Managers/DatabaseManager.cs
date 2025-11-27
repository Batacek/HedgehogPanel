using DotNetEnv;
using Npgsql;
using Serilog;

namespace HedgehogPanel.Core.Managers;

public class DatabaseManager
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(DatabaseManager));
    public readonly string _connectionString;
    private readonly string _dbName;
    private readonly string _dbUser;
    private readonly string _dbPass;
    private readonly string _dbHost;
    private readonly int _dbPort;

    private DatabaseManager(string connectionString, string dbName, string dbUser,
        string dbPass, string dbHost, int dbPort)
    {
        _connectionString = connectionString;
        _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        _dbUser = dbUser ?? throw new ArgumentNullException(nameof(dbUser));
        _dbPass = dbPass ?? throw new ArgumentNullException(nameof(dbPass));
        _dbHost = dbHost ?? throw new ArgumentNullException(nameof(dbHost));
        _dbPort = dbPort;
        Logger.Information("DatabaseManager created for host {Host}:{Port}, database {Database}, user {User}.", _dbHost, _dbPort, _dbName, _dbUser);
    }

    public static DatabaseManager Initialize()
    {
        Env.Load();

        string dbName = Env.GetString("DB_NAME");
        string dbUser = Env.GetString("DB_USER");
        string dbPass = Env.GetString("DB_PASSWORD");
        string dbHost = Env.GetString("DB_HOST", "localhost");
        int dbPort = Env.GetInt("DB_PORT", 5432);
        string connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPass};Database={dbName}";
        Logger.Information("Initializing DatabaseManager with host {Host}:{Port} and database {Database}.", dbHost, dbPort, dbName);
        
        return new DatabaseManager(connectionString, dbName, dbUser, dbPass, dbHost, dbPort);
    }
    
    public static readonly DatabaseManager Instance = Initialize();

    public NpgsqlConnection CreateConnection()
    {
        Logger.Debug("Creating new database connection to {Host}:{Port}/{Database} as {User}.", _dbHost, _dbPort, _dbName, _dbUser);
        return new NpgsqlConnection(_connectionString);
    }
}