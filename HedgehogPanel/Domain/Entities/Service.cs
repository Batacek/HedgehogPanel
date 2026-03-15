using System;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Domain.Entities;

public class Service
{
    public Guid Guid { get; private set; }
    public byte? LocalId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ServiceType Type { get; private set; }
    public Guid ServerGuid { get; private set; }

    public Service(Guid guid, string name, ServiceType type, Guid serverGuid, string? description = null, byte? localId = null)
    {
        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        ServerGuid = serverGuid;
        Description = description;
        LocalId = localId;
    }
}
