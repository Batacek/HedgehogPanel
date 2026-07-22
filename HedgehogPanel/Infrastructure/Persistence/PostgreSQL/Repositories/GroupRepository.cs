using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Exceptions;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GroupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Group?> GetByGuidAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, description FROM groups WHERE uuid = @id LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        if (!await reader.ReadAsync()) return null;
        return MapGroup(reader);
    }

    public async Task<Group?> GetByNameAsync(string name)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, description FROM groups WHERE name = @name LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@name", name);
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        if (!await reader.ReadAsync()) return null;
        return MapGroup(reader);
    }

    public async Task<IReadOnlyList<Group>> ListAsync(int limit, int offset)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, description FROM groups ORDER BY name LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);

        var list = new List<Group>();
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        while (await reader.ReadAsync())
        {
            list.Add(MapGroup(reader));
        }
        return list;
    }

    public async Task<bool> CreateAsync(Group group)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string checkSql = "SELECT EXISTS(SELECT 1 FROM groups WHERE name = @name)";
        await using var checkCmd = new NpgsqlCommand(checkSql, npgsqlConn);
        checkCmd.Parameters.AddWithValue("@name", group.Name);
        var existsObj = await DbExceptionGuard.ExecuteAsync(() => checkCmd.ExecuteScalarAsync());
        if (existsObj != null && (bool)existsObj)
        {
            throw new DuplicateEntityException("Group", "name", group.Name);
        }

        const string sql = "INSERT INTO groups (uuid, name, description) VALUES (@id, @name, @desc)";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", group.Guid);
        cmd.Parameters.AddWithValue("@name", group.Name);
        cmd.Parameters.AddWithValue("@desc", (object?)group.Description ?? DBNull.Value);
        return await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> UpdateAsync(Group group)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        // Reject a name that another group already uses (excluding this group).
        const string checkSql = "SELECT EXISTS(SELECT 1 FROM groups WHERE name = @name AND uuid <> @id)";
        await using var checkCmd = new NpgsqlCommand(checkSql, npgsqlConn);
        checkCmd.Parameters.AddWithValue("@name", group.Name);
        checkCmd.Parameters.AddWithValue("@id", group.Guid);
        var existsObj = await DbExceptionGuard.ExecuteAsync(() => checkCmd.ExecuteScalarAsync());
        if (existsObj != null && (bool)existsObj)
        {
            throw new DuplicateEntityException("Group", "name", group.Name);
        }

        const string sql = "UPDATE groups SET name = @name, description = @desc WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@name", group.Name);
        cmd.Parameters.AddWithValue("@desc", (object?)group.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", group.Guid);
        return await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM groups WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        return await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupGuid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT u.uuid, u.username, u.email, ug.priority
                             FROM users u
                             JOIN user_groups ug ON u.uuid = ug.user_uuid
                             WHERE ug.group_uuid = @g
                             ORDER BY ug.priority DESC, u.username";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@g", groupGuid);

        var list = new List<GroupMember>();
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        while (await reader.ReadAsync())
        {
            list.Add(new GroupMember(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }
        return list;
    }

    public async Task<bool> AddMemberAsync(Guid groupGuid, Guid userGuid, int priority)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"INSERT INTO user_groups (user_uuid, group_uuid, priority)
                             VALUES (@u, @g, @p)
                             ON CONFLICT (user_uuid, group_uuid) DO UPDATE SET priority = EXCLUDED.priority";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", userGuid);
        cmd.Parameters.AddWithValue("@g", groupGuid);
        cmd.Parameters.AddWithValue("@p", priority);
        return await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupGuid, Guid userGuid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM user_groups WHERE group_uuid = @g AND user_uuid = @u";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@g", groupGuid);
        cmd.Parameters.AddWithValue("@u", userGuid);
        return await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
    }

    private static Group MapGroup(NpgsqlDataReader reader)
    {
        return new Group(
            guid: reader.GetGuid(0),
            name: reader.GetString(1),
            description: reader.IsDBNull(2) ? null : reader.GetString(2));
    }
}
