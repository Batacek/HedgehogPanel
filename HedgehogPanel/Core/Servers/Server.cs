using HedgehogPanel.Services;
using HedgehogPanel.UserManagment;
using Serilog;

namespace HedgehogPanel.Servers;

public class Server
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(Server));
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; private set; }
    public ServerConfig Config { get; private set; }
    public Services.Service[] Services { get; private set; }
    public UserManagment.Account? OwnerAccount { get; private set; }
    public UserManagment.Group? OwnerGroup { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal Server(Guid guid, byte id, string name, ServerConfig config, Service[] services, Account? ownerAccount, Group? ownerGroup, DateTime createdAt)
    {
        GUID = guid;
        Id = id;
        Name = name;
        Config = config;
        Services = services;
        OwnerAccount = ownerAccount;
        OwnerGroup = ownerGroup;
        CreatedAt = createdAt;
        Logger.Information("Server created: {Name} (GUID: {Guid}, Id: {Id}, OwnerAccount: {OwnerAccount}, OwnerGroup: {OwnerGroup}, CreatedAt: {CreatedAt})", Name, GUID, Id, OwnerAccount?.Username, OwnerGroup?.Name, CreatedAt);
    }
}