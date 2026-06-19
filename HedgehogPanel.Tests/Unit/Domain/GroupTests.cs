using System;
using HedgehogPanel.Domain.Entities;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Domain;

public class GroupTests
{
    [Fact]
    public void Constructor_WithNullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Group(Guid.NewGuid(), null!));
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var id = Guid.NewGuid();
        var group = new Group(id, "admin", "Administrators", localId: null, priority: 10);

        Assert.Equal(id, group.Guid);
        Assert.Equal("admin", group.Name);
        Assert.Equal("Administrators", group.Description);
        Assert.Equal(10, group.Priority);
    }

    [Fact]
    public void Constructor_DefaultsPriorityToZero()
    {
        var group = new Group(Guid.NewGuid(), "users");
        Assert.Equal(0, group.Priority);
    }
}
