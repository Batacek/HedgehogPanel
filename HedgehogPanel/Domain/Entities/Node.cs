using System;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Domain.Entities;

public class Node
{
    public Guid Guid { get; private set; }
    public string Name { get; private set; }
    public string IpAddress { get; private set; }
    public int Port { get; private set; }
    public string? Description { get; private set; }
    public NodeStatus Status { get; private set; }
    public Guid? DaemonUuid { get; private set; }
    public string? DaemonVersion { get; private set; }
    public string? ProtocolVersion { get; private set; }
    public string? DaemonToken { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastSeen { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public Node(Guid guid, string name, string ipAddress, int port, string? description = null, NodeStatus status = NodeStatus.Unpaired, Guid? daemonUuid = null, string? daemonVersion = null, string? protocolVersion = null, string? daemonToken = null, string? lastError = null, DateTime? lastSeen = null, DateTime? createdAt = null)
    {
        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
        Description = description;
        Status = status;
        DaemonUuid = daemonUuid;
        DaemonVersion = daemonVersion;
        ProtocolVersion = protocolVersion;
        DaemonToken = daemonToken;
        LastError = lastError;
        LastSeen = lastSeen;
        CreatedAt = createdAt;
    }

    public bool IsPaired => DaemonToken != null;

    public void UpdateStatus(NodeStatus newStatus)
    {
        Status = newStatus;
        LastSeen = DateTime.UtcNow;
    }

    // The methods below return a changed copy instead of changing this node. Repositories
    // cache node instances and hand the same one to every request, so a change made in
    // place would be visible before it is stored, and would stay even if storing it fails.

    /// <summary>
    /// A copy paired with the given daemon. The token replaces any earlier one. The daemon
    /// version is kept only when the same daemon is paired again, because it describes that daemon.
    /// </summary>
    public Node WithPairing(Guid daemonUuid, string daemonToken, string protocolVersion, DateTime seenAt) =>
        new(guid: Guid, name: Name, ipAddress: IpAddress, port: Port, description: Description,
            status: NodeStatus.Online,
            daemonUuid: daemonUuid,
            daemonVersion: daemonUuid == DaemonUuid ? DaemonVersion : null,
            protocolVersion: protocolVersion,
            daemonToken: daemonToken ?? throw new ArgumentNullException(nameof(daemonToken)),
            lastError: null,
            lastSeen: seenAt,
            createdAt: CreatedAt);

    /// <summary>A copy recording that the daemon answered, and what it reported.</summary>
    public Node WithContact(NodeStatus status, string daemonVersion, string protocolVersion, DateTime seenAt) =>
        new(guid: Guid, name: Name, ipAddress: IpAddress, port: Port, description: Description,
            status: status,
            daemonUuid: DaemonUuid,
            daemonVersion: daemonVersion,
            protocolVersion: protocolVersion,
            daemonToken: DaemonToken,
            lastError: null,
            lastSeen: seenAt,
            createdAt: CreatedAt);

    /// <summary>A copy recording that the daemon could not be used. When it was last seen stays as it was.</summary>
    public Node WithFailure(NodeStatus status, string error) =>
        new(guid: Guid, name: Name, ipAddress: IpAddress, port: Port, description: Description,
            status: status,
            daemonUuid: DaemonUuid,
            daemonVersion: DaemonVersion,
            protocolVersion: ProtocolVersion,
            daemonToken: DaemonToken,
            lastError: error,
            lastSeen: LastSeen,
            createdAt: CreatedAt);
}
