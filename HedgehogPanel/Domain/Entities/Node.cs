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
}
