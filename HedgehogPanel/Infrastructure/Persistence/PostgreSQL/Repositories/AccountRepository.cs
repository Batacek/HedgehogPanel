using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Domain.Entities;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AccountRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Account?> GetByGuidAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users WHERE uuid = @id LIMIT 1";
        
        string uname, email;
        string? first, middle, last;
        uint xmin;

        await using (var cmd = new NpgsqlCommand(sql, npgsqlConn))
        {
            cmd.Parameters.AddWithValue("@id", guid);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) return null;
                
                uname = reader.GetString(1);
                email = reader.GetString(2);
                first = reader.IsDBNull(3) ? null : reader.GetString(3);
                middle = reader.IsDBNull(4) ? null : reader.GetString(4);
                last = reader.IsDBNull(5) ? null : reader.GetString(5);
                xmin = (uint)reader.GetInt32(6);
            }
        }

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        return new Account(guid, uname, email, true, null, first, middle, last, groups, xmin);
    }

    public async Task<Account?> GetByUsernameAsync(string username)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users WHERE username = @u LIMIT 1";
        
        Guid guid;
        string uname, email;
        string? first, middle, last;
        uint xmin;

        await using (var cmd = new NpgsqlCommand(sql, npgsqlConn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) return null;
                
                guid = reader.GetGuid(0);
                uname = reader.GetString(1);
                email = reader.GetString(2);
                first = reader.IsDBNull(3) ? null : reader.GetString(3);
                middle = reader.IsDBNull(4) ? null : reader.GetString(4);
                last = reader.IsDBNull(5) ? null : reader.GetString(5);
                xmin = (uint)reader.GetInt32(6);
            }
        }

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        return new Account(guid, uname, email, true, null, first, middle, last, groups, xmin);
    }

    public async Task<IReadOnlyList<Account>> ListAsync(int limit, int offset)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users ORDER BY username LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var accounts = new List<Account>();
        var userDatas = new List<(Guid guid, string uname, string email, string? first, string? middle, string? last, uint xmin)>();
        
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                userDatas.Add((
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    (uint)reader.GetInt32(6)
                ));
            }
        }

        foreach (var ud in userDatas)
        {
            var groups = await LoadUserGroupsAsync(ud.guid, npgsqlConn);
            accounts.Add(new Account(ud.guid, ud.uname, ud.email, true, null, ud.first, ud.middle, ud.last, groups, ud.xmin));
        }

        return accounts;
    }

    public async Task<bool> CreateAsync(Account account, string passwordHash)
    {
         using var conn = await _connectionFactory.CreateConnectionAsync();
         if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
         
         const string sql = @"INSERT INTO users (uuid, username, email, password_hash, firstname, middlename, lastname)
                               VALUES (@id, @u, @e, crypt(@p, gen_salt('bf')), @f, @m, @l)";
         await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
         cmd.Parameters.AddWithValue("@id", account.Guid);
         cmd.Parameters.AddWithValue("@u", account.Username);
         cmd.Parameters.AddWithValue("@e", account.Email);
         cmd.Parameters.AddWithValue("@p", passwordHash);
         cmd.Parameters.AddWithValue("@f", (object?)account.FirstName ?? DBNull.Value);
         cmd.Parameters.AddWithValue("@m", (object?)account.MiddleName ?? DBNull.Value);
         cmd.Parameters.AddWithValue("@l", (object?)account.LastName ?? DBNull.Value);

         return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Account account, string? newPasswordHash = null)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        string sql = @"UPDATE users SET username = @u, email = @e, firstname = @f, middlename = @m, lastname = @l";
        if (newPasswordHash != null) sql += ", password_hash = crypt(@p, gen_salt('bf'))";
        sql += " WHERE uuid = @id";
        
        if (account.RowVersion > 0) sql += " AND xmin::text::int = @v";

        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", account.Username);
        cmd.Parameters.AddWithValue("@e", account.Email);
        cmd.Parameters.AddWithValue("@f", (object?)account.FirstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)account.MiddleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object?)account.LastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", account.Guid);
        if (newPasswordHash != null) cmd.Parameters.AddWithValue("@p", newPasswordHash);
        if (account.RowVersion > 0) cmd.Parameters.AddWithValue("@v", (int)account.RowVersion);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM users WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    
    // Pomocná metoda pro autentizaci, protože hashování je v DB
    public async Task<Account?> AuthenticateAsync(string username, string password)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users
                              WHERE username = @u
                                AND password_hash = crypt(@p, password_hash)
                              LIMIT 1";
        
        Guid guid;
        string uname, email;
        string? first, middle, last;
        uint xmin;

        await using (var cmd = new NpgsqlCommand(sql, npgsqlConn))
        {
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) return null;

                guid = reader.GetGuid(0);
                uname = reader.GetString(1);
                email = reader.GetString(2);
                first = reader.IsDBNull(3) ? null : reader.GetString(3);
                middle = reader.IsDBNull(4) ? null : reader.GetString(4);
                last = reader.IsDBNull(5) ? null : reader.GetString(5);
                xmin = (uint)reader.GetInt32(6);
            }
        }

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        return new Account(guid, uname, email, true, null, first, middle, last, groups, xmin);
    }


    private async Task<Group[]> LoadUserGroupsAsync(Guid userGuid, NpgsqlConnection connection)
    {
        const string sql = @"SELECT g.uuid, g.name, g.description, ug.priority
                              FROM user_groups ug
                              JOIN groups g ON ug.group_uuid = g.uuid
                              WHERE ug.user_uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", userGuid);
        var groups = new List<Group>();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                groups.Add(new Group(
                    reader.GetGuid(0), // guid
                    reader.GetString(1), // name
                    reader.IsDBNull(2) ? null : reader.GetString(2), // description
                    null, // localId (not in DB yet)
                    (byte)reader.GetInt32(3) // priority
                ));
            }
        }
        return groups.ToArray();
    }
}
