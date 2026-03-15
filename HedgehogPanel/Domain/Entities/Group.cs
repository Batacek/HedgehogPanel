using System;

namespace HedgehogPanel.Domain.Entities;

public class Group
{
    public Guid Guid { get; private set; }
    public byte? LocalId { get; private set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public byte Priority { get; set; }

    public Group(Guid guid, string name, string? description = null, byte? localId = null, byte priority = 0)
    {
        Guid = guid;
        LocalId = localId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Priority = priority;
    }
}
