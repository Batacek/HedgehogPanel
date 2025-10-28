namespace HedgehogPanel.Servers;

public class Server
{
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; private set; }
    public ServerConfig Config { get; private set; }
    public Services.Service[] Services { get; private set; }
    public UserManagment.Account? OwnerAccount { get; private set; }
    public UserManagment.Group? OwnerGroup { get; private set; }
    public DateTime CreatedAt { get; private set; }
}