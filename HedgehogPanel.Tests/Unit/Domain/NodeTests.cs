using System;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Domain;

public class NodeTests
{
    [Fact]
    public void UpdateStatus_ChangesStatusAndStampsLastSeen()
    {
        var node = new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051);
        Assert.Null(node.LastSeen);

        node.UpdateStatus(NodeStatus.Online);

        Assert.Equal(NodeStatus.Online, node.Status);
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
    public void Constructor_DefaultsToUnpairedWithNoDaemonState()
    {
        var node = new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051);

        Assert.Equal(NodeStatus.Unpaired, node.Status);
        Assert.Null(node.DaemonUuid);
        Assert.Null(node.DaemonVersion);
        Assert.Null(node.ProtocolVersion);
        Assert.Null(node.DaemonToken);
        Assert.Null(node.LastError);
        Assert.False(node.IsPaired);
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var id = Guid.NewGuid();
        var daemonUuid = Guid.NewGuid();
        var node = new Node(id, "node", "10.0.0.1", 50051, description: "desc", status: NodeStatus.Online,
            daemonUuid: daemonUuid, daemonVersion: "0.1.0", protocolVersion: "2.0.0",
            daemonToken: "token", lastError: "timed out");

        Assert.Equal(id, node.Guid);
        Assert.Equal("node", node.Name);
        Assert.Equal("10.0.0.1", node.IpAddress);
        Assert.Equal(50051, node.Port);
        Assert.Equal("desc", node.Description);
        Assert.Equal(NodeStatus.Online, node.Status);
        Assert.Equal(daemonUuid, node.DaemonUuid);
        Assert.Equal("0.1.0", node.DaemonVersion);
        Assert.Equal("2.0.0", node.ProtocolVersion);
        Assert.Equal("token", node.DaemonToken);
        Assert.Equal("timed out", node.LastError);
    }

    [Fact]
    public void IsPaired_FollowsWhetherThereIsAToken()
    {
        Assert.False(new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051).IsPaired);
        Assert.True(new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051, daemonToken: "token").IsPaired);
    }
}
