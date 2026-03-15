using System;
using System.Linq;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Domain.Entities;

public class Account
{
    public Guid Guid { get; private set; }
    public byte? LocalId { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public string? FirstName { get; private set; }
    public string? MiddleName { get; private set; }
    public string? LastName { get; private set; }
    public Group[] Groups { get; private set; }
    public uint RowVersion { get; set; }

    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public bool IsAdmin => Groups?.Any(g => string.Equals(g.Name, "admin", StringComparison.OrdinalIgnoreCase)) ?? false;

    public Account(Guid guid, string username, string email, bool isActive = true, byte? localId = null, string? firstName = null, string? middleName = null, string? lastName = null, Group[]? groups = null, uint rowVersion = 0)
    {
        Guid = guid;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        IsActive = isActive;
        LocalId = localId;
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        Groups = groups ?? Array.Empty<Group>();
        RowVersion = rowVersion;
    }

    public void Rename(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername)) throw new ArgumentException("Username cannot be empty", nameof(newUsername));
        Username = newUsername;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
