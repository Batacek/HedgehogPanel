using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

/// <summary>
/// Resolves the database connection settings for the test run from the project's <c>.env</c>
/// file (the same file the application uses). Host, port, user, password and the JWT secret are
/// taken verbatim from <c>.env</c>; the database NAME is intentionally suffixed with
/// <c>_test</c> so the destructive fixture operations (TRUNCATE, schema reset) can never touch
/// the production database. Values fall back to sensible defaults if <c>.env</c> is missing.
/// </summary>
public static class TestDatabaseConfig
{
    public static string Host { get; }
    public static int Port { get; }
    public static string Username { get; }
    public static string Password { get; }
    public static string Database { get; }
    public static string JwtSecret { get; }

    public static string ConnectionString =>
        $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database}";

    /// <summary>Connection to the server's default <c>postgres</c> database (used to create the test DB).</summary>
    public static string MasterConnectionString =>
        $"Host={Host};Port={Port};Username={Username};Password={Password};Database=postgres";

    static TestDatabaseConfig()
    {
        var env = LoadDotEnv();

        Host = Value(env, "DB_HOST", "localhost");
        Port = int.TryParse(Value(env, "DB_PORT", "5432"), out var port) ? port : 5432;
        Username = Value(env, "DB_USER", "postgres");
        Password = Value(env, "DB_PASSWORD", "");
        JwtSecret = Value(env, "JWT_SECRET", "test-jwt-secret-key-for-integration-tests-0123456789");

        var name = Value(env, "DB_NAME", "hedgehogdb");
        Database = name.EndsWith("_test", StringComparison.OrdinalIgnoreCase) ? name : name + "_test";

        // The database name is interpolated into DDL (CREATE DATABASE), so guard against anything
        // that is not a plain identifier.
        if (!Regex.IsMatch(Database, "^[A-Za-z0-9_]+$"))
            throw new InvalidOperationException($"Resolved test database name '{Database}' is not a valid identifier.");
    }

    private static string Value(IReadOnlyDictionary<string, string> env, string key, string fallback)
        => env.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    private static Dictionary<string, string> LoadDotEnv()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var path = FindEnvFile();
        if (path == null) return result;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();

            // Strip optional surrounding quotes.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            result[key] = value;
        }

        return result;
    }

    /// <summary>Walks up from the test binaries to the repository root and returns HedgehogPanel/.env if present.</summary>
    private static string? FindEnvFile()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "HedgehogPanel.sln")))
            {
                var envPath = Path.Combine(dir, "HedgehogPanel", ".env");
                return File.Exists(envPath) ? envPath : null;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
