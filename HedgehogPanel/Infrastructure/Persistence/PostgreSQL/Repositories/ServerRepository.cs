using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Domain.Entities;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ServerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Server?> GetByGuidAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, description, created_at FROM servers WHERE uuid = @id LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return MapServer(reader);
    }

    public async Task<IReadOnlyList<Server>> ListAsync(int limit, int offset)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, description, created_at FROM servers ORDER BY name LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var list = new List<Server>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapServer(reader));
        }
        return list;
    }

    public async Task<bool> CreateAsync(Server server)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "INSERT INTO servers (uuid, name, description) VALUES (@id, @n, @d)";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", server.Guid);
        cmd.Parameters.AddWithValue("@n", server.Name);
        cmd.Parameters.AddWithValue("@d", (object?)server.Description ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Server server)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "UPDATE servers SET name = @n, description = @d WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@n", server.Name);
        cmd.Parameters.AddWithValue("@d", (object?)server.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", server.Guid);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM servers WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IReadOnlyList<Server>> ListByOwnerAsync(Guid userGuid, int limit, int offset)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"
            SELECT s.uuid, s.name, s.description, s.created_at 
            FROM servers s
            JOIN server_owners so ON s.uuid = so.server_uuid
            WHERE so.user_uuid = @userGuid
            ORDER BY s.name 
            LIMIT @limit OFFSET @offset";
            
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@userGuid", userGuid);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var list = new List<Server>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapServer(reader));
        }
        return list;
    }

    public async Task<IReadOnlyList<Server>> ListUnownedAsync(int limit, int offset)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"
            SELECT s.uuid, s.name, s.description, s.created_at 
            FROM servers s
            LEFT JOIN server_owners so ON s.uuid = so.server_uuid
            WHERE so.server_uuid IS NULL
            ORDER BY s.name 
            LIMIT @limit OFFSET @offset";
            
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var list = new List<Server>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapServer(reader));
        }
        return list;
    }

    public async Task<string?> GetOwnerUsernameAsync(Guid serverGuid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"
            SELECT u.username
            FROM users u
            JOIN server_owners so ON u.uuid = so.user_uuid
            WHERE so.server_uuid = @serverGuid
            LIMIT 1";
            
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@serverGuid", serverGuid);
        
        return (string?)await cmd.ExecuteScalarAsync();
    }

    public async Task<bool> AssignToUserAsync(Guid serverGuid, Guid userGuid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"
            INSERT INTO server_owners (server_uuid, user_uuid)
            VALUES (@serverGuid, @userGuid)
            ON CONFLICT (server_uuid, user_uuid) WHERE user_uuid IS NOT NULL DO NOTHING";
            
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@serverGuid", serverGuid);
        cmd.Parameters.AddWithValue("@userGuid", userGuid);
        
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private Server MapServer(NpgsqlDataReader reader)
    {
        return new Server(
            guid: reader.GetGuid(0),
            name: reader.GetString(1),
            hostname: "", // Hostname (not in DB)
            port: 22, // Port (not in DB)
            status: Domain.Enums.ServerStatus.Unknown, // Status (not in DB)
            localId: null, // (not in DB yet)
            description: reader.IsDBNull(2) ? null : reader.GetString(2),
            lastSeen: null, // (not in DB yet)
            createdAt: reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
        );
    }
}
