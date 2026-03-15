using System;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Domain.Entities;

public class Server
{
    public Guid Guid { get; private set; }
    public byte? LocalId { get; private set; }
    public string Name { get; private set; }
    public string Hostname { get; private set; }
    public int Port { get; private set; }
    public ServerStatus Status { get; private set; }
    public string? Description { get; private set; }
    public DateTime? LastSeen { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public uint RowVersion { get; set; }

    public Server(Guid guid, string name, string hostname, int port = 22, ServerStatus status = ServerStatus.Unknown, byte? localId = null, string? description = null, DateTime? lastSeen = null, DateTime? createdAt = null, uint rowVersion = 0)
    {
        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
        Port = port;
        Status = status;
        LocalId = localId;
        Description = description;
        LastSeen = lastSeen;
        CreatedAt = createdAt;
        RowVersion = rowVersion;
    }

    public void UpdateStatus(ServerStatus newStatus)
    {
        Status = newStatus;
        LastSeen = DateTime.UtcNow;
    }
}
