namespace HedgehogPanel.UserManagment;

public class Group
{
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte Priority { get; set; }
    
    public Group(byte id, string name, string description, byte priority)
    {
        GUID = Managers.ID.Instance.GenerateGUID();
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Priority = priority;
    }
}