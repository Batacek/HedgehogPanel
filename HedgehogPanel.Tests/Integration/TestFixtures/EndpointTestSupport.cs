using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

/// <summary>Shared helpers for the endpoint integration tests: client creation,
/// user/admin seeding and login.</summary>
public static class EndpointTestSupport
{
    public const string Password = "Test_Password_123";

    /// <summary>Creates a client that does not auto-follow redirects, so status codes
    /// (e.g. the auth challenge redirect) can be asserted directly.</summary>
    public static HttpClient NewClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Inserts a fresh active user (unique username derived from <paramref name="prefix"/>).</summary>
    public static async Task<Account> SeedUserAsync(string connectionString, string prefix)
    {
        var username = Unique(prefix);
        var account = new Account(Guid.NewGuid(), username, $"{username}@hedgehog.batacek.eu");
        var repository = new AccountRepository(new EndpointTestConnectionFactory(connectionString));
        await repository.CreateAsync(account, Password);
        return account;
    }

    /// <summary>Inserts a fresh user and puts it into the "admin" group so it gains the Admin role.</summary>
    public static async Task<Account> SeedAdminAsync(string connectionString, string prefix)
    {
        var account = await SeedUserAsync(connectionString, prefix);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var groupId = Guid.NewGuid();
        await using (var groupCmd = new NpgsqlCommand(
            "INSERT INTO groups (uuid, name, description) VALUES (@id, 'admin', 'Administrators')", conn))
        {
            groupCmd.Parameters.AddWithValue("id", groupId);
            await groupCmd.ExecuteNonQueryAsync();
        }

        await using (var membershipCmd = new NpgsqlCommand(
            "INSERT INTO user_groups (user_uuid, group_uuid, priority) VALUES (@u, @g, 100)", conn))
        {
            membershipCmd.Parameters.AddWithValue("u", account.Guid);
            membershipCmd.Parameters.AddWithValue("g", groupId);
            await membershipCmd.ExecuteNonQueryAsync();
        }

        return account;
    }

    /// <summary>Logs the client in with the seeded password; throws if login does not succeed.</summary>
    public static async Task LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/login", new { username, password = Password });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Login failed ({(int)response.StatusCode}): {body}");
        }
    }

    private static string Unique(string prefix)
    {
        var value = $"{prefix}_{Guid.NewGuid():N}";
        return value.Length <= 60 ? value : value.Substring(0, 60);
    }
}

/// <summary>Opens a real connection to the test database for seeding.</summary>
public sealed class EndpointTestConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public EndpointTestConnectionFactory(string connectionString) => _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
