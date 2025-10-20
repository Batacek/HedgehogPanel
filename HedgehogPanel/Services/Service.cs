using HedgehogPanel.UserManagment;

namespace HedgehogPanel.Services;

public class Service
{
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; private set;  }
    public string Description { get; private set; }
    public byte type { get; private set; }
    public byte groupId { get; private set; }
    public UserManagment.Account OwnerAccount { get; private set; }
    public UserManagment.Group OwnerGroup { get; private set; }

    private protected Service(byte id, string name = null, string description = null, byte type = default,
        byte groupId = default, Account ownerAccount = null, Group ownerGroup = null)
    {
        GUID = Managers.ID.Instance.GenerateGUID();
        Id = id;
        Name = name ?? $"Service_{GUID}";
        Description = description;
        this.type = type;
        OwnerAccount = ownerAccount ?? throw new ArgumentNullException(nameof(ownerAccount));
        OwnerGroup = ownerGroup ?? throw new ArgumentNullException(nameof(ownerGroup));
    }
}