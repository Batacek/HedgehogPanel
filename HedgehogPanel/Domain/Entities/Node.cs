using System;

namespace HedgehogPanel.Domain.Entities;

public class Node
{
    public Guid Guid { get; private set; }
    public string Name { get; private set; }
    public string IpAddress { get; private set; }
    public int Port { get; private set; }
    public string? Description { get; private set; }
    public string? Status { get; private set; }
    public string? RegistrationToken { get; private set; }
    public DateTime? LastSeen { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public Node(Guid guid, string name, string ipAddress, int port, string? description = null, string? status = null, string? registrationToken = null, DateTime? lastSeen = null, DateTime? createdAt = null)
    {
        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
        Description = description;
        Status = status;
        RegistrationToken = registrationToken;
        LastSeen = lastSeen;
        CreatedAt = createdAt;
    }

    public void UpdateStatus(string newStatus)
    {
        Status = newStatus;
        LastSeen = DateTime.UtcNow;
    }
}
