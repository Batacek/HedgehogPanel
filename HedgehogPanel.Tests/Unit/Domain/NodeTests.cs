using System;
using HedgehogPanel.Domain.Entities;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Domain;

public class NodeTests
{
    [Fact]
    public void UpdateStatus_ChangesStatusAndStampsLastSeen()
    {
        var node = new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051);
        Assert.Null(node.LastSeen);

        node.UpdateStatus("Online");

        Assert.Equal("Online", node.Status);
        Assert.NotNull(node.LastSeen);
    }

    [Fact]
    public void Constructor_WithNullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Node(Guid.NewGuid(), null!, "10.0.0.1", 50051));
    }

    [Fact]
    public void Constructor_WithNullIpAddress_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Node(Guid.NewGuid(), "node", null!, 50051));
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var id = Guid.NewGuid();
        var node = new Node(id, "node", "10.0.0.1", 50051, description: "desc", status: "Online");

        Assert.Equal(id, node.Guid);
        Assert.Equal("node", node.Name);
        Assert.Equal("10.0.0.1", node.IpAddress);
        Assert.Equal(50051, node.Port);
        Assert.Equal("desc", node.Description);
        Assert.Equal("Online", node.Status);
    }
}
