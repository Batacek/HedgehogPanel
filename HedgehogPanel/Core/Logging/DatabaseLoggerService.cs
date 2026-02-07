using Npgsql;
using System.Text.Json;
using HedgehogPanel.Core.Database;
using System.Data;

namespace HedgehogPanel.Core.Logging;

public class DatabaseLoggerService : ILoggerService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseLoggerService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public void Debug(string message, params object[] args) { }
    public void Information(string message, params object[] args) { }
    public void Warning(string message, params object[] args) { }
    public void Warning(Exception ex, string message, params object[] args) { }
    public void Error(string message, params object[] args) { }
    public void Error(Exception ex, string message, params object[] args) { }
    public void Fatal(string message, params object[] args) { }
    public void Fatal(Exception ex, string message, params object[] args) { }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            using var conn = await _connectionFactory.CreateConnectionAsync();
            if (conn is not NpgsqlConnection npgsqlConn)
            {
                throw new InvalidOperationException("Expected NpgsqlConnection");
            }

            const string sql = @"
                INSERT INTO user_security_events (
                    id, user_id, actor_user_id, event_type, occurred_at_utc, 
                    ip_address, user_agent, success, metadata
                ) VALUES (
                    @id, @user_id, @actor_user_id, @event_type, @occurred_at_utc, 
                    @ip_address, @user_agent, @success, @metadata::jsonb
                )";

            await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
            cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@user_id", (object?)securityEvent.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@actor_user_id", (object?)securityEvent.ActorUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@event_type", securityEvent.EventType);
            cmd.Parameters.AddWithValue("@occurred_at_utc", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@ip_address", securityEvent.IpAddress);
            cmd.Parameters.AddWithValue("@user_agent", (object?)securityEvent.UserAgent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@success", securityEvent.Success);
            
            var metadataJson = securityEvent.Metadata != null 
                ? JsonSerializer.Serialize(securityEvent.Metadata) 
                : "{}";
            cmd.Parameters.AddWithValue("@metadata", metadataJson);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to log security event to database: {ex.Message}");
        }
    }
}
