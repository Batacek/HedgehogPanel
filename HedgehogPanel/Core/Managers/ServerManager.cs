using Npgsql;
using Serilog;

namespace HedgehogPanel.Core.Managers;

public static class ServerManager
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(ServerManager));

    public record ServerListItem(Guid Id, string Name, string? Description, DateTime CreatedAt, string? OwnerUsername);

    public static async Task<IReadOnlyList<ServerListItem>> ListServersAsync(int limit = 100, int offset = 0)
    {
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT DISTINCT ON (s.uuid) s.uuid, s.name, s.description, s.created_at, u.username
FROM servers s
LEFT JOIN server_owners so ON s.uuid = so.server_uuid AND so.user_uuid IS NOT NULL
LEFT JOIN users u ON so.user_uuid = u.uuid
ORDER BY s.uuid, so.assigned_at ASC
LIMIT @lim OFFSET @off";
        await using var cmd = new NpgsqlCommand(sql, conn);
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
        Logger.Information("Listed {Count} servers (limit={Limit}, offset={Offset}).", list.Count, limit, offset);
        return list;
    }

    public static async Task<ServerListItem> CreateServerAsync(string name, string? description = null, Guid? ownerUserGuid = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string insertServer = @"INSERT INTO servers (name, description) VALUES (@n, @d) RETURNING uuid, name, description, created_at";
            await using var cmd = new NpgsqlCommand(insertServer, conn, (NpgsqlTransaction)tx);
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
                await using var cmd2 = new NpgsqlCommand(insertOwner, conn, (NpgsqlTransaction)tx);
                cmd2.Parameters.AddWithValue("@s", id);
                cmd2.Parameters.AddWithValue("@u", ownerUserGuid.Value);
                await cmd2.ExecuteNonQueryAsync();

                const string getU = "SELECT username FROM users WHERE uuid = @u LIMIT 1";
                await using var cmd3 = new NpgsqlCommand(getU, conn, (NpgsqlTransaction)tx);
                cmd3.Parameters.AddWithValue("@u", ownerUserGuid.Value);
                ownerUsername = (string?)await cmd3.ExecuteScalarAsync();
            }

            await tx.CommitAsync();
            Logger.Information("Created server {Name} with id {Id}", retName, id);
            return new ServerListItem(id, retName, desc, createdAt, ownerUsername);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.Error(ex, "Failed to create server {Name}", name);
            throw;
        }
    }

    public static async Task<bool> DeleteServerAsync(Guid id)
    {
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = "DELETE FROM servers WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = await cmd.ExecuteNonQueryAsync();
        Logger.Warning(rows > 0 ? "Deleted server {Id}" : "Server {Id} not found", id);
        return rows > 0;
    }
}
