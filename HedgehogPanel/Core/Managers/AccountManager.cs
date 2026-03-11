using Npgsql;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Database;
using HedgehogPanel.UserManagment;
using HedgehogPanel.Servers;

namespace HedgehogPanel.Core.Managers;

public class AccountManager : IAccountManager
{
    private readonly ILoggerService _logger;
    private readonly IDbConnectionFactory _connectionFactory;

    public AccountManager(ILoggerService logger, IDbConnectionFactory connectionFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Account> AuthenticateAsync(string username, string password)
    {
        _logger.Debug("Authenticating user {Username}...", username);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException("Invalid username or password.");

        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users
                              WHERE username = @u
                                AND password_hash = encode(digest(@p, 'sha256'), 'hex')
                              LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", password);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.Warning("Authentication failed for user {Username}.", username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        var guid = reader.GetGuid(0);
        var xmin = (uint)reader.GetInt32(6);
        await reader.CloseAsync();

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, groups, xmin);
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        _logger.Information("User {Username} authenticated. Account GUID: {Guid}.", uname, guid);
        return acc;
    }

    public async Task<Account?> GetAccountByUsernameAsync(string username)
    {
        _logger.Debug("Fetching account by username {Username}...", username);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users WHERE username = @u LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", username);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.Information("No account found for username {Username}.", username);
            return null;
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        var guid = reader.GetGuid(0);
        var xmin = (uint)reader.GetInt32(6);
        await reader.CloseAsync();

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, groups, xmin);
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        _logger.Debug("Fetched account {Username} with GUID {Guid}.", uname, guid);
        return acc;
    }

    public async Task<Account?> GetAccountByIdAsync(Guid userId)
    {
        _logger.Debug("Fetching account by id {UserId}...", userId);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname, xmin::text::int
                              FROM users WHERE uuid = @id LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.Information("No account found for id {UserId}.", userId);
            return null;
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        var guid = reader.GetGuid(0);
        var xmin = (uint)reader.GetInt32(6);
        await reader.CloseAsync();

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new Account(guid, id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, groups, xmin);
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        _logger.Debug("Fetched account {Username} with GUID {Guid} by id.", uname, guid);
        return acc;
    }

    public async Task<Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username required", nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("password required", nameof(password));

        _logger.Information("Creating account for username {Username}...", username);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
        const string sql = @"INSERT INTO users (username, email, firstname, middlename, lastname, password_hash)
                              VALUES (@u, @e, @f, @m, @l, encode(digest(@p, 'sha256'), 'hex'))
                              RETURNING uuid, username, email, firstname, middlename, lastname";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
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
        await reader.CloseAsync();

        var groups = await LoadUserGroupsAsync(guid, npgsqlConn);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new Account(guid, id, uname, emailOut, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, groups);
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        _logger.Information("Created account {Username} with GUID {Guid}.", uname, guid);
        return acc;
    }

    public async Task<bool> UpdateAccountAsync(string username, string newEmail, string? firstName = null, string? middleName = null, string? lastName = null, string? newPassword = null, string? ip = "unknown", Guid? actorGuid = null, uint? expectedVersion = null)
    {
        _logger.Information("Updating account {Username}...", username);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
        const string sql = @"UPDATE users
                              SET email = @e,
                                  firstname = @f,
                                  middlename = @m,
                                  lastname = @l,
                                  password_hash = CASE WHEN @p IS NULL THEN password_hash ELSE encode(digest(@p, 'sha256'), 'hex') END
                              WHERE username = @u AND (@expectedVersion IS NULL OR xmin::text::int = @expectedVersion)
                              RETURNING uuid";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@e", newEmail);
        cmd.Parameters.AddWithValue("@f", (object?)firstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)middleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object?)lastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", (object?)newPassword ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@expectedVersion", expectedVersion.HasValue ? (object)expectedVersion.Value : DBNull.Value);
        
        var userUuid = await cmd.ExecuteScalarAsync() as Guid?;
        var success = userUuid != null;

        if (success)
        {
            _logger.Information("Updated account {Username}.", username);
            if (newPassword != null)
            {
                await _logger.LogSecurityEventAsync(new SecurityEvent(
                    "User.Password.Changed",
                    userUuid,
                    actorGuid,
                    ip ?? "unknown",
                    null,
                    true,
                    new { initiatedBy = actorGuid != null ? "admin" : "user" }
                ));
            }
        }
        else
            _logger.Warning("No rows updated for account {Username}.", username);
        return success;
    }

    public async Task<bool> DeleteAccountAsync(string username)
    {
        _logger.Warning("Deleting account {Username}...", username);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
        const string sql = @"DELETE FROM users WHERE username = @u";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", username);
        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows > 0)
            _logger.Warning("Deleted account {Username}.", username);
        else
            _logger.Warning("No account deleted for {Username}.", username);
        return rows > 0;
    }

    public async Task<IReadOnlyList<Account>> ListAccountsAsync(int limit = 100, int offset = 0)
    {
        _logger.Debug("Listing accounts with limit {Limit} and offset {Offset}...", limit, offset);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname FROM users ORDER BY created_at DESC LIMIT @lim OFFSET @off";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@lim", limit);
        cmd.Parameters.AddWithValue("@off", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var userDataList = new List<(Guid guid, string uname, string email, string? first, string? middle, string? last)>();
        while (await reader.ReadAsync())
        {
            var guid = reader.GetGuid(0);
            var uname = reader.GetString(1);
            var email = reader.GetString(2);
            var first = reader.IsDBNull(3) ? null : reader.GetString(3);
            var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
            var last = reader.IsDBNull(5) ? null : reader.GetString(5);
            userDataList.Add((guid, uname, email, first, middle, last));
        }
        await reader.CloseAsync();

        var list = new List<Account>();
        foreach (var userData in userDataList)
        {
            var groups = await LoadUserGroupsAsync(userData.guid, npgsqlConn);
            byte id = string.Equals(userData.uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
            var acc = new Account(userData.guid, id, userData.uname, userData.email, userData.first ?? string.Empty, userData.middle ?? string.Empty, userData.last ?? string.Empty, groups);
            acc.Name = string.Join(" ", new[] { userData.first, userData.middle, userData.last }.Where(s => !string.IsNullOrWhiteSpace(s)));
            list.Add(acc);
        }
        _logger.Information("Listed {Count} accounts (limit={Limit}, offset={Offset}).", list.Count, limit, offset);
        return list;
    }
    public async Task<List<Server>> GetServerListAsync(Guid userId)
    {
        _logger.Debug("Fetching server list for user {UserId}...", userId);
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sqlSelectIds = @"SELECT s.uuid, s.name, s.description, s.created_at, s.xmin::text::int
                                    FROM servers s
                                    INNER JOIN server_owners so ON s.uuid = so.server_uuid
                                    WHERE so.user_uuid = @u";
        await using var cmd = new NpgsqlCommand(sqlSelectIds, npgsqlConn);
        cmd.Parameters.AddWithValue("@u", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Server>();
        while (await reader.ReadAsync())
        {
            var serverUuid = reader.GetGuid(0);
            var name = reader.GetString(1);
            var createdAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);
            var xmin = (uint)reader.GetInt32(4);
            var server = new Server(serverUuid, 0, name, new ServerConfig(), Array.Empty<HedgehogPanel.Services.Service>(), null, null, createdAt, xmin);
            list.Add(server);
        }
        _logger.Information("Fetched {Count} servers for user {UserId}.", list.Count, userId);
        return list;
    }

    private async Task<Group[]> LoadUserGroupsAsync(Guid userGuid, NpgsqlConnection connection)
    {
        const string sql = @"SELECT g.uuid, g.name, g.description
                             FROM groups g
                             INNER JOIN user_groups ug ON g.uuid = ug.group_uuid
                             WHERE ug.user_uuid = @userGuid";
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userGuid", userGuid);
        await using var reader = await cmd.ExecuteReaderAsync();
        var groups = new List<Group>();
        byte groupId = 0;
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            var description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            // Priority is not stored in DB, using default value 0
            groups.Add(new Group(groupId++, name, description, 0));
        }
        return groups.ToArray();
    }
}