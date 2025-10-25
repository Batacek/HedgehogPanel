using Npgsql;

namespace HedgehogPanel.Managers;

public static class AccountManager
{
    public static async Task<UserManagment.Account> AuthenticateAsync(string username, string password)
    {
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
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);

        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return acc;
    }

    public static async Task<UserManagment.Account?> GetAccountByUsernameAsync(string username)
    {
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT uuid, username, email, firstname, middlename, lastname
                              FROM users WHERE username = @u LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var first = reader.IsDBNull(3) ? null : reader.GetString(3);
        var middle = reader.IsDBNull(4) ? null : reader.GetString(4);
        var last = reader.IsDBNull(5) ? null : reader.GetString(5);
        var email = reader.GetString(2);
        var uname = reader.GetString(1);
        byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return acc;
    }

    public static async Task<UserManagment.Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username required", nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("password required", nameof(password));

        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"INSERT INTO users (username, email, firstname, middlename, lastname, password_hash)
                              VALUES (@u, @e, @f, @m, @l, encode(digest(@p, 'sha256'), 'hex'))
                              RETURNING username, email, firstname, middlename, lastname";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@f", (object?)firstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)middleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object?)lastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", password);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var first = reader.IsDBNull(2) ? null : reader.GetString(2);
        var middle = reader.IsDBNull(3) ? null : reader.GetString(3);
        var last = reader.IsDBNull(4) ? null : reader.GetString(4);
        byte id = string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
        var acc = new UserManagment.Account(id, username, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
        acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return acc;
    }

    public static async Task<bool> UpdateAccountAsync(string username, string newEmail, string? firstName = null, string? middleName = null, string? lastName = null, string? newPassword = null)
    {
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
        return rows > 0;
    }

    public static async Task<bool> DeleteAccountAsync(string username)
    {
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"DELETE FROM users WHERE username = @u";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", username);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public static async Task<IReadOnlyList<UserManagment.Account>> ListAccountsAsync(int limit = 100, int offset = 0)
    {
        await using var conn = DatabaseManager.Instance.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT username, email, firstname, middlename, lastname FROM users ORDER BY created_at DESC LIMIT @lim OFFSET @off";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lim", limit);
        cmd.Parameters.AddWithValue("@off", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<UserManagment.Account>();
        while (await reader.ReadAsync())
        {
            var uname = reader.GetString(0);
            var email = reader.GetString(1);
            var first = reader.IsDBNull(2) ? null : reader.GetString(2);
            var middle = reader.IsDBNull(3) ? null : reader.GetString(3);
            var last = reader.IsDBNull(4) ? null : reader.GetString(4);
            byte id = string.Equals(uname, "admin", StringComparison.OrdinalIgnoreCase) ? (byte)0 : (byte)1;
            var acc = new UserManagment.Account(id, uname, email, first ?? string.Empty, middle ?? string.Empty, last ?? string.Empty, Array.Empty<UserManagment.Group>());
            acc.Name = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
            list.Add(acc);
        }
        return list;
    }
}