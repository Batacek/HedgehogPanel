using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Database;

namespace HedgehogPanel.Core.Managers;

public class ServerManager : IServerManager
{
    private readonly ILoggerService _logger;
    private readonly IDbConnectionFactory _connectionFactory;

    public ServerManager(ILoggerService logger, IDbConnectionFactory connectionFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public record ServerListItem(Guid Id, string Name, string? Description, DateTime CreatedAt, string? OwnerUsername);

    public async Task<IReadOnlyList<ServerListItem>> ListServersAsync(int limit = 100, int offset = 0)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT DISTINCT ON (s.uuid) s.uuid, s.name, s.description, s.created_at, u.username
FROM servers s
LEFT JOIN server_owners so ON s.uuid = so.server_uuid AND so.user_uuid IS NOT NULL
LEFT JOIN users u ON so.user_uuid = u.uuid
ORDER BY s.uuid, so.assigned_at ASC
LIMIT @lim OFFSET @off";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@lim", limit);
        cmd.Parameters.AddWithValue("@off", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ServerListItem>();
        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? null : reader.GetString(2);
            var createdAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);
            var owner = reader.IsDBNull(4) ? null : reader.GetString(4);
            list.Add(new ServerListItem(id, name, desc, createdAt, owner));
        }
        _logger.Information("Listed {Count} servers (limit={Limit}, offset={Offset}).", list.Count, limit, offset);
        return list;
    }

    public async Task<ServerListItem> CreateServerAsync(string name, string? description = null, Guid? ownerUserGuid = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        await using var tx = await npgsqlConn.BeginTransactionAsync();
        try
        {
            const string insertServer = @"INSERT INTO servers (name, description) VALUES (@n, @d) RETURNING uuid, name, description, created_at";
            await using var cmd = new NpgsqlCommand(insertServer, npgsqlConn, (NpgsqlTransaction)tx);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetGuid(0);
            var retName = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? null : reader.GetString(2);
            var createdAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);
            await reader.DisposeAsync();

            string? ownerUsername = null;
            if (ownerUserGuid.HasValue)
            {
                const string insertOwner = @"INSERT INTO server_owners (server_uuid, user_uuid, group_uuid) VALUES (@s, @u, NULL)";
                await using var cmd2 = new NpgsqlCommand(insertOwner, npgsqlConn, (NpgsqlTransaction)tx);
                cmd2.Parameters.AddWithValue("@s", id);
                cmd2.Parameters.AddWithValue("@u", ownerUserGuid.Value);
                await cmd2.ExecuteNonQueryAsync();

                const string getU = "SELECT username FROM users WHERE uuid = @u LIMIT 1";
                await using var cmd3 = new NpgsqlCommand(getU, npgsqlConn, (NpgsqlTransaction)tx);
                cmd3.Parameters.AddWithValue("@u", ownerUserGuid.Value);
                ownerUsername = (string?)await cmd3.ExecuteScalarAsync();
            }

            await tx.CommitAsync();
            _logger.Information("Created server {Name} with id {Id}", retName, id);
            return new ServerListItem(id, retName, desc, createdAt, ownerUsername);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.Error(ex, "Failed to create server {Name}", name);
            throw;
        }
    }

    public async Task<bool> DeleteServerAsync(Guid id)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM servers WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = await cmd.ExecuteNonQueryAsync();
        _logger.Warning(rows > 0 ? "Deleted server {Id}" : "Server {Id} not found", id);
        return rows > 0;
    }
}
