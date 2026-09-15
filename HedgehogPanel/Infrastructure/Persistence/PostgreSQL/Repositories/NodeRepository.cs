using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Persistence.Store;
using Npgsql;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Exceptions;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IInMemoryStore _store;
    private readonly HedgehogConfig _config;

    public NodeRepository(IDbConnectionFactory connectionFactory, IInMemoryStore store, HedgehogConfig config)
    {
        _connectionFactory = connectionFactory;
        _store = store;
        _config = config;
    }

    public async Task<Node?> GetByGuidAsync(Guid guid)
    {
        if (_config.Cache.Enabled)
        {
            var cached = _store.Get<Node>(guid);
            if (cached != null) return cached;
        }

        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, ip_address, port, description, status, daemon_uuid, daemon_version, protocol_version, daemon_token, last_error, last_seen, created_at FROM nodes WHERE uuid = @id LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        if (!await reader.ReadAsync()) return null;

        var node = MapNode(reader);
        
        if (_config.Cache.Enabled && node != null)
        {
            _store.Set(guid, node);
        }

        return node;
    }

    public async Task<IReadOnlyList<Node>> ListAsync(int limit, int offset)
    {
        if (_config.Cache.Enabled)
        {
            var relationKey = $"NodesList:{limit}:{offset}";
            var nodeIds = _store.GetRelation(relationKey);

            if (nodeIds.Count > 0)
            {
                var nodes = new List<Node>();
                bool allFound = true;
                foreach (var id in nodeIds)
                {
                    var n = _store.Get<Node>(id);
                    if (n == null)
                    {
                        allFound = false;
                        break;
                    }
                    nodes.Add(n);
                }

                if (allFound) return nodes;
            }
        }

        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "SELECT uuid, name, ip_address, port, description, status, daemon_uuid, daemon_version, protocol_version, daemon_token, last_error, last_seen, created_at FROM nodes ORDER BY name LIMIT @limit OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var list = new List<Node>();
        await using var reader = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteReaderAsync());
        while (await reader.ReadAsync())
        {
            var node = MapNode(reader);
            list.Add(node);
            
            if (_config.Cache.Enabled)
            {
                _store.Set(node.Guid, node);
            }
        }
        
        if (_config.Cache.Enabled && list.Count > 0)
        {
            var relationKey = $"NodesList:{limit}:{offset}";
            _store.SetRelation(relationKey, list.Select(n => n.Guid));
        }
        
        return list;
    }

    public async Task<bool> CreateAsync(Node node)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        // Check if node with same name already exists
        const string checkSql = @"SELECT EXISTS(SELECT 1 FROM nodes WHERE name = @name)";
        await using var checkCmd = new NpgsqlCommand(checkSql, npgsqlConn);
        checkCmd.Parameters.AddWithValue("@name", node.Name);
        var existsObj = await DbExceptionGuard.ExecuteAsync(() => checkCmd.ExecuteScalarAsync());
        if (existsObj != null && (bool)existsObj)
        {
            throw new DuplicateEntityException("Node", "name", node.Name);
        }

        const string sql = "INSERT INTO nodes (uuid, name, ip_address, port, description, status, daemon_uuid, daemon_version, protocol_version, daemon_token, last_error, last_seen) VALUES (@id, @name, @ip, @port, @desc, @status, @daemonUuid, @daemonVersion, @protocolVersion, @token, @lastError, @lastSeen)";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", node.Guid);
        cmd.Parameters.AddWithValue("@name", node.Name);
        cmd.Parameters.AddWithValue("@ip", node.IpAddress);
        cmd.Parameters.AddWithValue("@port", node.Port);
        cmd.Parameters.AddWithValue("@desc", (object?)node.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", node.Status.ToString());
        cmd.Parameters.AddWithValue("@daemonUuid", (object?)node.DaemonUuid ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@daemonVersion", (object?)node.DaemonVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@protocolVersion", (object?)node.ProtocolVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@token", (object?)node.DaemonToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastError", (object?)node.LastError ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastSeen", (object?)node.LastSeen ?? DBNull.Value);

        var result = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
        
        if (result && _config.Cache.Enabled)
        {
            _store.Set(node.Guid, node);
            InvalidateListCache();
        }
        
        return result;
    }

    public async Task<bool> UpdateAsync(Node node)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");
        
        // Check whether another node already has this name; the node being updated keeps its own.
        const string checkSql = @"SELECT EXISTS(SELECT 1 FROM nodes WHERE name = @name AND uuid <> @id)";
        await using var checkCmd = new NpgsqlCommand(checkSql, npgsqlConn);
        checkCmd.Parameters.AddWithValue("@name", node.Name);
        checkCmd.Parameters.AddWithValue("@id", node.Guid);
        var existsObj = await DbExceptionGuard.ExecuteAsync(() => checkCmd.ExecuteScalarAsync());
        if (existsObj != null && (bool)existsObj)
        {
            throw new DuplicateEntityException("Node", "name", node.Name);
        }
        
        const string sql = "UPDATE nodes SET name = @name, ip_address = @ip, port = @port, description = @desc, status = @status, daemon_uuid = @daemonUuid, daemon_version = @daemonVersion, protocol_version = @protocolVersion, daemon_token = @token, last_error = @lastError, last_seen = @lastSeen WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@name", node.Name);
        cmd.Parameters.AddWithValue("@ip", node.IpAddress);
        cmd.Parameters.AddWithValue("@port", node.Port);
        cmd.Parameters.AddWithValue("@desc", (object?)node.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", node.Status.ToString());
        cmd.Parameters.AddWithValue("@daemonUuid", (object?)node.DaemonUuid ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@daemonVersion", (object?)node.DaemonVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@protocolVersion", (object?)node.ProtocolVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@token", (object?)node.DaemonToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastError", (object?)node.LastError ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastSeen", (object?)node.LastSeen ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", node.Guid);

        var result = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
        
        if (result && _config.Cache.Enabled)
        {
            _store.Remove<Node>(node.Guid);
            _store.Set(node.Guid, node);
            InvalidateListCache();
        }
        
        return result;
    }

    public async Task<bool> DeleteAsync(Guid guid)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync();
        if (conn is not NpgsqlConnection npgsqlConn) throw new InvalidOperationException("Expected NpgsqlConnection");

        const string sql = "DELETE FROM nodes WHERE uuid = @id";
        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        cmd.Parameters.AddWithValue("@id", guid);
        var result = await DbExceptionGuard.ExecuteAsync(() => cmd.ExecuteNonQueryAsync()) > 0;
        
        if (result && _config.Cache.Enabled)
        {
            _store.Remove<Node>(guid);
            InvalidateListCache();
        }
        
        return result;
    }

    private Node MapNode(NpgsqlDataReader reader)
    {
        return new Node(
            guid: reader.GetGuid(0),
            name: reader.GetString(1),
            ipAddress: reader.GetString(2),
            port: reader.GetInt32(3),
            description: reader.IsDBNull(4) ? null : reader.GetString(4),
            status: Enum.Parse<NodeStatus>(reader.GetString(5)),
            daemonUuid: reader.IsDBNull(6) ? (Guid?)null : reader.GetGuid(6),
            daemonVersion: reader.IsDBNull(7) ? null : reader.GetString(7),
            protocolVersion: reader.IsDBNull(8) ? null : reader.GetString(8),
            daemonToken: reader.IsDBNull(9) ? null : reader.GetString(9),
            lastError: reader.IsDBNull(10) ? null : reader.GetString(10),
            lastSeen: reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
            createdAt: reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12)
        );
    }

    private void InvalidateListCache()
    {
        // Invalidate all possible list cache keys
        // Since we don't know all possible limit/offset combinations, we use a simple pattern
        // In production, you might want to track all active keys or use a more sophisticated approach
        for (int limit = 10; limit <= 1000; limit += 10)
        {
            for (int offset = 0; offset <= 100; offset += 10)
            {
                var relationKey = $"NodesList:{limit}:{offset}";
                _store.RemoveRelation(relationKey);
            }
        }
    }
}
