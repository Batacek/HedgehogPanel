using Npgsql;
using Serilog;

namespace HedgehogPanel.Core.Managers;

public static class AccountManager
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(AccountManager));
    public static async Task<UserManagment.Account> AuthenticateAsync(string username, string password)
    {
        Logger.Debug("Authenticating user {Username}...", username);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException("Invalid username or password.");

        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname
                              FROM users
                              WHERE username = @u
                                AND password_hash = encode(digest(@p, 'sha256'), 'hex')
                              LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", password);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Logger.Warning("Authentication failed for user {Username}.", username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        var guid = reader.GetGuid(0);

        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Logger.Information("User {Username} authenticated. Account GUID: {Guid}.", uname, guid);
        return acc;
    }

    public static async Task<UserManagment.Account?> GetAccountByUsernameAsync(string username)
    {
        Logger.Debug("Fetching account by username {Username}...", username);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname
                              FROM users WHERE username = @u LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Logger.Information("No account found for username {Username}.", username);
            return null;
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        var guid = reader.GetGuid(0);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Logger.Debug("Fetched account {Username} with GUID {Guid}.", uname, guid);
        return acc;
    }

    public static async Task<UserManagment.Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username required", nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("password required", nameof(password));

        Logger.Information("Creating account for username {Username}...", username);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"INSERT INTO users (username, email, firstname, middlename, lastname, password_hash)
                              VALUES (@u, @e, @f, @m, @l, encode(digest(@p, 'sha256'), 'hex'))
                              RETURNING uuid, username, email, firstname, middlename, lastname";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@f", (object?)firstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)middleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object?)lastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", password);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var guid = reader.GetGuid(0);
        var uname = reader.GetString(1);
        var emailOut = reader.GetString(2);
        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(guid, id, uname, emailOut, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Logger.Information("Created account {Username} with GUID {Guid}.", uname, guid);
        return acc;
    }

    public static async Task<bool> UpdateAccountAsync(string username, string newEmail, string? firstName = null, string? middleName = null, string? lastName = null, string? newPassword = null)
    {
        Logger.Information("Updating account {Username}...", username);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"UPDATE users
                              SET email = @e,
                                  firstname = @f,
                                  middlename = @m,
                                  lastname = @l,
                                  password_hash = CASE WHEN @p IS NULL THEN password_hash ELSE encode(digest(@p, 'sha256'), 'hex') END
                              WHERE username = @u";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@e", newEmail);
        cmd.Parameters.AddWithValue("@f", (object?)firstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)middleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object?)lastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", (object?)newPassword ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", username);
        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows > 0)
            Logger.Information("Updated account {Username}.", username);
        else
            Logger.Warning("No rows updated for account {Username}.", username);
        return rows > 0;
    }

    public static async Task<bool> DeleteAccountAsync(string username)
    {
        Logger.Warning("Deleting account {Username}...", username);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"DELETE FROM users WHERE username = @u";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows > 0)
            Logger.Warning("Deleted account {Username}.", username);
        else
            Logger.Warning("No account deleted for {Username}.", username);
        return rows > 0;
    }

    public static async Task<IReadOnlyList<UserManagment.Account>> ListAccountsAsync(int limit = 100, int offset = 0)
    {
        Logger.Debug("Listing accounts with limit {Limit} and offset {Offset}...", limit, offset);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname FROM users ORDER BY created_at DESC LIMIT @lim OFFSET @off";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lim", limit);
        cmd.Parameters.AddWithValue("@off", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<UserManagment.Account>();
        while (await reader.ReadAsync())
        {
            var guid = reader.GetGuid(0);
            var uname = reader.GetString(1);
            var email = reader.GetString(2);
            var first = reader.IsDBNull(3) ? null : reader.GetString(3);
            var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
            var last = reader.IsDBNull(5) ? null : reader.GetString(5);
            byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
            var acc = new UserManagment.Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
            acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
            list.Add(acc);
        }
        Logger.Information("Listed {Count} accounts (limit={Limit}, offset={Offset}).", list.Count, limit, offset);
        return list;
    }
    public static async Task<List<Servers.Server>> GetServerListAsync(Guid userId)
    {
        Logger.Debug("Fetching server list for user {UserId}...", userId);
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sqlSelectIds = @"SELECT s.uuid, s.name, s.description, s.created_at
                                    FROM servers s
                                    INNER JOIN server_owners so ON s.uuid = so.server_uuid
                                    WHERE so.user_uuid = @u";
        await using var cmd = new NpgsqlCommand(sqlSelectIds, conn);
        cmd.Parameters.AddWithValue("@u", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Servers.Server>();
        while (await reader.ReadAsync())
        {
            var serverUuid = reader.GetGuid(0);
            var name = reader.GetString(1);
            var createdAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);
            var server = new Servers.Server(serverUuid, 0, name, new Servers.ServerConfig(), Array.Empty<Services.Service>(), null, null, createdAt);
            list.Add(server);
        }
        Logger.Information("Fetched {Count} servers for user {UserId}.", list.Count, userId);
        return list;
    }
}