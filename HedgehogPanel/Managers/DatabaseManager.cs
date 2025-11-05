using Npgsql;
using DotNetEnv;

namespace HedgehogPanel.Managers;

public class DatabaseManager
{
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
        
        return new DatabaseManager(connectionString, dbName, dbUser, dbPass, dbHost, dbPort);
    }
    
    public static readonly DatabaseManager Instance = Initialize();

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}