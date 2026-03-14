using System;
using HedgehogPanel.Services;
using HedgehogPanel.UserManagment;
using HedgehogPanel.Core.Logging;

namespace HedgehogPanel.Servers;

public class Server
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(Server));
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; private set; }
    public ServerConfig Config { get; private set; }
    public Services.Service[] Services { get; private set; }
    public UserManagment.Account[] OwnerAccounts { get; private set; }
    public UserManagment.Group[] OwnerGroups { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public uint RowVersion { get; set; }

    internal Server(Guid guid, byte id, string name, ServerConfig config, Service[] services, Account[] ownerAccounts, Group[] ownerGroups, DateTime createdAt, uint rowVersion = 0)
    {
        GUID = guid;
        Id = id;
        Name = name;
        Config = config;
        Services = services;
        OwnerAccounts = ownerAccounts ?? Array.Empty<Account>();
        OwnerGroups = ownerGroups ?? Array.Empty<Group>();
        CreatedAt = createdAt;
        RowVersion = rowVersion;
        Logger.Information("Server created: {Name} (GUID: {Guid}, Id: {Id}, OwnerAccounts: {OwnerAccountCount}, OwnerGroups: {OwnerGroupCount}, CreatedAt: {CreatedAt})", Name, GUID, Id, OwnerAccounts.Length, OwnerGroups.Length, CreatedAt);
    }
}