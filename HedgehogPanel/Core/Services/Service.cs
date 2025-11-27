using HedgehogPanel.Core.Managers;
using HedgehogPanel.UserManagment;
using Serilog;

namespace HedgehogPanel.Services;

public class Service
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(Service));
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
        GUID = ID.Instance.GenerateGUID();
        Id = id;
        Name = name ?? $"Service_{GUID}";
        Description = description;
        this.type = type;
        OwnerAccount = ownerAccount ?? throw new ArgumentNullException(nameof(ownerAccount));
        OwnerGroup = ownerGroup ?? throw new ArgumentNullException(nameof(ownerGroup));
        Logger.Information("Service created: {Name} (GUID: {Guid}, Id: {Id}, Type: {Type}, GroupId: {GroupId}, Owner: {Owner})", Name, GUID, Id, this.type, this.groupId, OwnerAccount.Username);
    }
}