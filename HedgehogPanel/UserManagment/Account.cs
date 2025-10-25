namespace HedgehogPanel.UserManagment;

public class Account
{
    public Guid GUID { get; private set; }
    public byte Id { get; private set; }
    public string Username { get; private set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public Group[] Groups { get; set; }
    public bool IsAdmin { get; private set; }

    internal Account(byte id, string username, string email, string firstName, string middleName, string lastName, Group[] groups)
    {
        GUID = Managers.ID.Instance.GenerateGUID();
        Id = id;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
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