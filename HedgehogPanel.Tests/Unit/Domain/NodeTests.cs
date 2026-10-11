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

    private static Node PairedNode(Guid daemonUuid) =>
        new(Guid.NewGuid(), "node", "10.0.0.1", 50051, description: "desc", status: NodeStatus.Offline,
            daemonUuid: daemonUuid, daemonVersion: "0.1.0", protocolVersion: "2.0.0",
            daemonToken: "old-token", lastError: "timed out", lastSeen: new DateTime(2026, 1, 1),
            createdAt: new DateTime(2025, 12, 1));

    [Fact]
    public void WithPairing_StoresTheDaemonAndLeavesTheOriginalAlone()
    {
        var node = new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051, description: "desc");
        var daemonUuid = Guid.NewGuid();
        var seenAt = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc);

        var paired = node.WithPairing(daemonUuid, "token", "2.0.0", seenAt);

        Assert.Equal(node.Guid, paired.Guid);
        Assert.Equal("desc", paired.Description);
        Assert.Equal(NodeStatus.Online, paired.Status);
        Assert.Equal(daemonUuid, paired.DaemonUuid);
        Assert.Equal("token", paired.DaemonToken);
        Assert.Equal("2.0.0", paired.ProtocolVersion);
        Assert.Equal(seenAt, paired.LastSeen);
        Assert.Null(paired.LastError);

        // Cached nodes are shared between requests, so the original must not change.
        Assert.Equal(NodeStatus.Unpaired, node.Status);
        Assert.Null(node.DaemonToken);
    }

    [Fact]
    public void WithPairing_TheSameDaemonAgain_KeepsItsVersion()
    {
        var daemonUuid = Guid.NewGuid();

        var paired = PairedNode(daemonUuid).WithPairing(daemonUuid, "new-token", "2.0.0", DateTime.UtcNow);

        Assert.Equal("0.1.0", paired.DaemonVersion);
        Assert.Equal("new-token", paired.DaemonToken);
    }

    [Fact]
    public void WithPairing_ADifferentDaemon_ForgetsTheOldVersion()
    {
        var paired = PairedNode(Guid.NewGuid()).WithPairing(Guid.NewGuid(), "new-token", "2.0.0", DateTime.UtcNow);

        Assert.Null(paired.DaemonVersion);
    }

    [Fact]
    public void WithContact_RecordsWhatTheDaemonReportedAndClearsTheError()
    {
        var daemonUuid = Guid.NewGuid();
        var node = PairedNode(daemonUuid);
        var seenAt = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc);

        var contacted = node.WithContact(NodeStatus.Online, "0.2.0", "2.1.0", seenAt);

        Assert.Equal(NodeStatus.Online, contacted.Status);
        Assert.Equal("0.2.0", contacted.DaemonVersion);
        Assert.Equal("2.1.0", contacted.ProtocolVersion);
        Assert.Equal(seenAt, contacted.LastSeen);
        Assert.Null(contacted.LastError);
        Assert.Equal(daemonUuid, contacted.DaemonUuid);
        Assert.Equal("old-token", contacted.DaemonToken);
        Assert.Equal("timed out", node.LastError);
    }

    [Fact]
    public void WithFailure_RecordsTheErrorAndKeepsLastSeen()
    {
        var node = PairedNode(Guid.NewGuid());

        var failed = node.WithFailure(NodeStatus.Offline, "unreachable");

        Assert.Equal(NodeStatus.Offline, failed.Status);
        Assert.Equal("unreachable", failed.LastError);
        Assert.Equal(node.LastSeen, failed.LastSeen);
        Assert.Equal(node.DaemonVersion, failed.DaemonVersion);
        Assert.Equal("old-token", failed.DaemonToken);
        Assert.Equal(node.CreatedAt, failed.CreatedAt);
    }
}
