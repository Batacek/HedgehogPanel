using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public PostgreSqlFixture()
    {
        // Connection settings come from the project's .env file (database name is suffixed
        // with _test so the production database is never touched). See TestDatabaseConfig.
        ConnectionString = TestDatabaseConfig.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        var testDbName = TestDatabaseConfig.Database;

        // Create test database if it doesn't exist
        try
        {
            await using var masterConnection = new NpgsqlConnection(TestDatabaseConfig.MasterConnectionString);
            await masterConnection.OpenAsync();

            // Check if test database exists
            await using var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", masterConnection);
            checkCmd.Parameters.AddWithValue("name", testDbName);
            var exists = await checkCmd.ExecuteScalarAsync();

            if (exists == null)
            {
                // Create test database (identifier is validated in TestDatabaseConfig).
                await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{testDbName}\"", masterConnection);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            // Database already exists (race condition), ignore
        }

        // Always ensure schema is applied (check if users table exists)
        await using var testConnection = new NpgsqlConnection(ConnectionString);
        await testConnection.OpenAsync();
        await using var tableCheckCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users'",
            testConnection);
        var tableCount = (long)(await tableCheckCmd.ExecuteScalarAsync() ?? 0L);
        if (tableCount == 0)
        {
            await ApplySchemaAsync();
        }
    }

    public Task DisposeAsync()
    {
        // Keep test database for reuse
        return Task.CompletedTask;
    }

    private async Task ApplySchemaAsync()
    {
        var projectRoot = FindProjectRoot();
        var sqlPath = Path.Combine(projectRoot, "create.sql");
        
        if (!File.Exists(sqlPath))
        {
            throw new FileNotFoundException($"SQL schema file not found at: {sqlPath}");
        }

        var sql = await File.ReadAllTextAsync(sqlPath);
        
        // Remove CREATE EXTENSION line to avoid duplicate key error if extension already exists
        var lines = sql.Split('\n');
        var filteredLines = System.Linq.Enumerable.Where(lines, l => !l.TrimStart().StartsWith("CREATE EXTENSION"));
        sql = string.Join('\n', filteredLines);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string FindProjectRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "HedgehogPanel.sln")))
            {
                return directory;
            }
            directory = Directory.GetParent(directory)?.FullName;
        }
        
        throw new InvalidOperationException("Could not find project root (HedgehogPanel.sln)");
    }

    public async Task CleanDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var tables = new[]
        {
            "service_owners", "server_owners", "services", "servers", 
            "nodes", "user_groups", "groups", "users", "user_security_events"
        };

        foreach (var table in tables)
        {
            var checkSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @table";
            await using var checkCmd = new NpgsqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("table", table);
            var count = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L);
            if (count > 0)
            {
                await using var command = new NpgsqlCommand($"TRUNCATE TABLE {table} CASCADE", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
