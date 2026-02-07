using HedgehogPanel.Core.Managers;
using HedgehogPanel.Core.Logging;

namespace HedgehogPanel.UserManagment;

public class Group
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(Group));
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte Priority { get; set; }
    
    public Group(byte id, string name, string description, byte priority)
    {
        GUID = ID.Instance.GenerateGUID();
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Priority = priority;
        Logger.Information("Group created: {Name} (GUID: {Guid}, Id: {Id}, Priority: {Priority})", Name, GUID, Id, Priority);
    }
}