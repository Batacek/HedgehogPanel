using System;
using HedgehogPanel.Core.Managers;
using HedgehogPanel.UserManagment;
using HedgehogPanel.Core.Logging;

namespace HedgehogPanel.Services;

public class Service
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(Service));
    public Guid Guid { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; private set;  }
    public string Description { get; private set; }
    public byte Type { get; private set; }
    public byte GroupId { get; private set; }
    public UserManagment.Account[] OwnerAccounts { get; private set; }
    public UserManagment.Group[] OwnerGroups { get; private set; }

    private protected Service(Guid guid, byte id, string name = null, string description = null, byte type = default,
        byte groupId = default, Account[] ownerAccounts = null, Group[] ownerGroups = null)
    {
        Guid = guid;
        Id = id;
        Name = name ?? $"Service_{Guid}";
        Description = description;
        // TODO: Use an enum to represent service types.
        Type = type;
        GroupId = groupId;
        OwnerAccounts = ownerAccounts ?? Array.Empty<Account>();
        OwnerGroups = ownerGroups ?? Array.Empty<Group>();
        Logger.Information("Service created: {Name} (GUID: {Guid}, Id: {Id}, Type: {Type}, GroupId: {GroupId}, OwnerAccounts: {OwnerAccountCount}, OwnerGroups: {OwnerGroupCount})", Name, Guid, Id, this.Type, this.GroupId, OwnerAccounts.Length, OwnerGroups.Length);
    }
}