using System;
using HedgehogPanel.Domain.Entities;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Domain;

public class AccountTests
{
    private static Account NewAccount(
        string? first = null, string? middle = null, string? last = null,
        Group[]? groups = null, bool isActive = true)
        => new(Guid.NewGuid(), "user", "user@hedgehog.batacek.eu", isActive, null, first, middle, last, groups);

    [Fact]
    public void FullName_WithAllParts_JoinsWithSingleSpaces()
    {
        var account = NewAccount("John", "Q", "Public");
        Assert.Equal("John Q Public", account.FullName);
    }

    [Fact]
    public void FullName_WithMissingMiddle_OmitsIt()
    {
        var account = NewAccount("John", null, "Public");
        Assert.Equal("John Public", account.FullName);
    }

    [Fact]
    public void FullName_WithWhitespaceOnlyPart_OmitsIt()
    {
        var account = NewAccount("John", "   ", "Public");
        Assert.Equal("John Public", account.FullName);
    }

    [Fact]
    public void FullName_WithNoNameParts_ReturnsEmpty()
    {
        var account = NewAccount();
        Assert.Equal(string.Empty, account.FullName);
    }

    [Fact]
    public void IsAdmin_WhenInAdminGroup_ReturnsTrue()
    {
        var account = NewAccount(groups: new[] { new Group(Guid.NewGuid(), "admin") });
        Assert.True(account.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenAdminGroupHasDifferentCasing_ReturnsTrue()
    {
        var account = NewAccount(groups: new[] { new Group(Guid.NewGuid(), "ADMIN") });
        Assert.True(account.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenOnlyNonAdminGroups_ReturnsFalse()
    {
        var account = NewAccount(groups: new[]
        {
            new Group(Guid.NewGuid(), "users"),
            new Group(Guid.NewGuid(), "operators")
        });
        Assert.False(account.IsAdmin);
    }

    [Fact]
    public void IsAdmin_WhenNoGroups_ReturnsFalse()
    {
        var account = NewAccount();
        Assert.False(account.IsAdmin);
    }

    [Fact]
    public void Constructor_WithoutGroups_DefaultsToEmpty()
    {
        var account = NewAccount();
        Assert.NotNull(account.Groups);
        Assert.Empty(account.Groups);
    }

    [Fact]
    public void Constructor_WithNullUsername_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Account(Guid.NewGuid(), null!, "user@hedgehog.batacek.eu"));
    }

    [Fact]
    public void Constructor_WithNullEmail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Account(Guid.NewGuid(), "user", null!));
    }

    [Fact]
    public void Rename_WithValidUsername_UpdatesUsername()
    {
        var account = NewAccount();
        account.Rename("newname");
        Assert.Equal("newname", account.Username);
    }

    [Fact]
    public void Rename_WithEmptyUsername_Throws()
    {
        var account = NewAccount();
        Assert.Throws<ArgumentException>(() => account.Rename(""));
    }

    [Fact]
    public void Rename_WithWhitespaceUsername_Throws()
    {
        var account = NewAccount();
        Assert.Throws<ArgumentException>(() => account.Rename("   "));
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var account = NewAccount(isActive: false);
        account.Activate();
        Assert.True(account.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var account = NewAccount(isActive: true);
        account.Deactivate();
        Assert.False(account.IsActive);
    }
}
