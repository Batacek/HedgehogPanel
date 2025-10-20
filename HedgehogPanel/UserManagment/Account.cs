namespace HedgehogPanel.UserManagment;

public class Account
{
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public Group[] Groups { get; set; }
    public bool IsAdmin { get; private set; }

    internal Account(byte id, string username, string email, string name, Group[] groups)
    {
        GUID = Managers.ID.Instance.GenerateGUID();
        Id = id;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Groups = groups ?? throw new ArgumentNullException(nameof(groups));
        
        if (Id == 0)
        {
            IsAdmin = true;
        }
        else
        {
            IsAdmin = false;
        }
    }
}