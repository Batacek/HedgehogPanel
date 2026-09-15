using System;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Domain.Entities;

public class Server
{
    public Guid Guid { get; private set; }
    public byte? LocalId { get; private set; }
    public string Name { get; private set; }
    public string Hostname { get; private set; }
    public int DaemonPort { get; private set; }
    public ServerStatus Status { get; private set; }
    public string? Description { get; private set; }
    public DateTime? LastSeen { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public uint RowVersion { get; set; }

    /// <summary>
    /// The node whose daemon manages this server, or null when no daemon runs on it.
    /// A dedicated server hosting virtual ones typically has none of its own.
    /// </summary>
    public Guid? NodeUuid { get; private set; }

    public Server(Guid guid, string name, string hostname, int daemonPort = 22, ServerStatus status = ServerStatus.Unknown, byte? localId = null, string? description = null, DateTime? lastSeen = null, DateTime? createdAt = null, uint rowVersion = 0, Guid? nodeUuid = null)
    {
        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
        DaemonPort = daemonPort;
        Status = status;
        LocalId = localId;
        Description = description;
        LastSeen = lastSeen;
        CreatedAt = createdAt;
        RowVersion = rowVersion;
        NodeUuid = nodeUuid;
    }

    /// <summary>
    /// Points this server at the node running its daemon, or clears the link with null.
    /// </summary>
    public void AssignNode(Guid? nodeUuid)
    {
        NodeUuid = nodeUuid;
    }

    public void UpdateStatus(ServerStatus newStatus)
    {
        Status = newStatus;
        LastSeen = DateTime.UtcNow;
    }
}
