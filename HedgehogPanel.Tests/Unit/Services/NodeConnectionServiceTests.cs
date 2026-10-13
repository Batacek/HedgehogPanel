using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Exceptions;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Services;

public class NodeConnectionServiceTests
{
    private const string Code = "ABCD-EFGH";

    private readonly Mock<INodeRepository> _nodes = new();
    private readonly Mock<INodeDaemonService> _daemons = new();
    private readonly NodeConnectionService _service;

    private Node? _savedPairing;
    private Node? _savedState;

    public NodeConnectionServiceTests()
    {
        _service = new NodeConnectionService(_nodes.Object, _daemons.Object);
        _nodes.Setup(n => n.SavePairingAsync(It.IsAny<Node>()))
            .Callback<Node>(node => _savedPairing = node)
            .ReturnsAsync(true);
        _nodes.Setup(n => n.SaveDaemonStateAsync(It.IsAny<Node>()))
            .Callback<Node>(node => _savedState = node)
            .ReturnsAsync(true);
    }

    private Node Stored(Node node)
    {
        _nodes.Setup(n => n.GetByGuidAsync(node.Guid)).ReturnsAsync(node);
        return node;
    }

    private Node UnpairedNode() => Stored(new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051));

    private Node PairedNode() => Stored(new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051,
        status: NodeStatus.Online, daemonUuid: Guid.NewGuid(), daemonVersion: "0.1.0",
        protocolVersion: "2.0.0", daemonToken: "token", lastSeen: new DateTime(2026, 1, 1)));

    private void ProbeReturns(string protocolVersion, ProtocolCompatibility compatibility) =>
        _daemons.Setup(d => d.ProbeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonProbe(protocolVersion, compatibility, TimeSpan.FromSeconds(5)));

    private void HandshakeThrows(DaemonFailure failure) =>
        _daemons.Setup(d => d.HandshakeAsync(It.IsAny<Node>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(failure, $"failed: {failure}"));

    // ---------- pairing ----------

    [Fact]
    public async Task Pair_StoresTheDaemonAndToken()
    {
        var node = UnpairedNode();
        var daemonUuid = Guid.NewGuid();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(daemonUuid, "issued-token"));

        var result = await _service.PairAsync(node.Guid, Code);

        Assert.Equal(ProtocolCompatibility.Full, result.Compatibility);
        Assert.NotNull(_savedPairing);
        Assert.Equal(node.Guid, _savedPairing.Guid);
        Assert.Equal(daemonUuid, _savedPairing.DaemonUuid);
        Assert.Equal("issued-token", _savedPairing.DaemonToken);
        Assert.Equal("2.0.0", _savedPairing.ProtocolVersion);
        Assert.Equal(NodeStatus.Online, _savedPairing.Status);
        Assert.NotNull(_savedPairing.LastSeen);
        Assert.Same(_savedPairing, result.Node);
    }

    [Fact]
    public async Task Pair_TrimsTheCode()
    {
        var node = UnpairedNode();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(Guid.NewGuid(), "token"));

        await _service.PairAsync(node.Guid, $"  {Code}\n");

        _daemons.Verify(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCD-EFGH-ABCD-EFGH-ABCD-EFGH-ABCD")]
    public async Task Pair_WithoutAUsableCode_ContactsNothing(string? code)
    {
        var node = UnpairedNode();

        await Assert.ThrowsAsync<ValidationException>(() => _service.PairAsync(node.Guid, code!));

        _daemons.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Pair_AnUnknownNode_Throws()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.PairAsync(Guid.NewGuid(), Code));

        _daemons.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Pair_AnIncompatibleDaemon_IsRefusedBeforeTheCodeIsSpent()
    {
        var node = UnpairedNode();
        ProbeReturns("3.0.0", ProtocolCompatibility.Incompatible);

        var ex = await Assert.ThrowsAsync<DaemonException>(() => _service.PairAsync(node.Guid, Code));

        Assert.Equal(DaemonFailure.Incompatible, ex.Failure);
        Assert.Contains("3.0.0", ex.Message);
        _daemons.Verify(d => d.RegisterAsync(It.IsAny<Node>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(_savedPairing);
    }

    [Fact]
    public async Task Pair_ALimitedDaemon_IsPaired()
    {
        var node = UnpairedNode();
        ProbeReturns("2.1.0", ProtocolCompatibility.Limited);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(Guid.NewGuid(), "token"));

        var result = await _service.PairAsync(node.Guid, Code);

        Assert.Equal(ProtocolCompatibility.Limited, result.Compatibility);
        Assert.NotNull(_savedPairing);
    }

    [Fact]
    public async Task Pair_ARejectedCode_StoresNothing()
    {
        var node = UnpairedNode();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(DaemonFailure.InvalidOneTimeCode, "wrong code"));

        var ex = await Assert.ThrowsAsync<DaemonException>(() => _service.PairAsync(node.Guid, Code));

        Assert.Equal(DaemonFailure.InvalidOneTimeCode, ex.Failure);
        _nodes.Verify(n => n.SavePairingAsync(It.IsAny<Node>()), Times.Never);
        _nodes.Verify(n => n.SaveDaemonStateAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task Pair_AnAlreadyPairedNode_ReplacesTheToken()
    {
        var node = PairedNode();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(node.DaemonUuid!.Value, "fresh-token"));

        await _service.PairAsync(node.Guid, Code);

        Assert.Equal("fresh-token", _savedPairing!.DaemonToken);
        Assert.Equal("0.1.0", _savedPairing.DaemonVersion);
    }

    [Fact]
    public async Task Pair_ADaemonPairedAsAnotherNode_IsADuplicate()
    {
        var node = UnpairedNode();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(Guid.NewGuid(), "token"));
        _nodes.Setup(n => n.SavePairingAsync(It.IsAny<Node>()))
            .ThrowsAsync(new DatabaseConstraintException("nodes_daemon_uuid_key", new Exception()));

        await Assert.ThrowsAsync<DuplicateEntityException>(() => _service.PairAsync(node.Guid, Code));
    }

    [Fact]
    public async Task Pair_ANodeDeletedMeanwhile_IsNotFound()
    {
        var node = UnpairedNode();
        ProbeReturns("2.0.0", ProtocolCompatibility.Full);
        _daemons.Setup(d => d.RegisterAsync(node, Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonPairing(Guid.NewGuid(), "token"));
        _nodes.Setup(n => n.SavePairingAsync(It.IsAny<Node>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.PairAsync(node.Guid, Code));
    }

    // ---------- ping ----------

    [Fact]
    public async Task Ping_StoresWhatTheDaemonReported()
    {
        var node = PairedNode();
        _daemons.Setup(d => d.HandshakeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonHandshake(node.DaemonUuid!.Value, "0.2.0", "2.0.0", ProtocolCompatibility.Full));

        var result = await _service.PingAsync(node.Guid);

        Assert.NotNull(_savedState);
        Assert.Equal(NodeStatus.Online, _savedState.Status);
        Assert.Equal("0.2.0", _savedState.DaemonVersion);
        Assert.Equal("token", _savedState.DaemonToken);
        Assert.True(_savedState.LastSeen > node.LastSeen);
        Assert.Same(_savedState, result.Node);
        Assert.Equal(ProtocolCompatibility.Full, result.Compatibility);
    }

    [Fact]
    public async Task Ping_AnIncompatibleDaemon_MarksTheNodeIncompatible()
    {
        var node = PairedNode();
        _daemons.Setup(d => d.HandshakeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DaemonHandshake(node.DaemonUuid!.Value, "1.0.0", "3.0.0", ProtocolCompatibility.Incompatible));

        var result = await _service.PingAsync(node.Guid);

        Assert.Equal(NodeStatus.Incompatible, _savedState!.Status);
        Assert.Equal("3.0.0", _savedState.ProtocolVersion);
        Assert.Equal(ProtocolCompatibility.Incompatible, result.Compatibility);
    }

    [Theory]
    [InlineData(DaemonFailure.Unreachable, NodeStatus.Offline)]
    [InlineData(DaemonFailure.Timeout, NodeStatus.Offline)]
    [InlineData(DaemonFailure.InvalidAddress, NodeStatus.Offline)]
    [InlineData(DaemonFailure.Unauthenticated, NodeStatus.Unknown)]
    [InlineData(DaemonFailure.IdentityMismatch, NodeStatus.Unknown)]
    [InlineData(DaemonFailure.NotSupported, NodeStatus.Unknown)]
    [InlineData(DaemonFailure.Unexpected, NodeStatus.Unknown)]
    public async Task Ping_AFailure_IsStoredAndRethrown(DaemonFailure failure, NodeStatus expected)
    {
        var node = PairedNode();
        HandshakeThrows(failure);

        var ex = await Assert.ThrowsAsync<DaemonException>(() => _service.PingAsync(node.Guid));

        Assert.Equal(failure, ex.Failure);
        Assert.NotNull(_savedState);
        Assert.Equal(expected, _savedState.Status);
        Assert.Equal(ex.Message, _savedState.LastError);
        Assert.Equal(node.LastSeen, _savedState.LastSeen);
    }

    [Fact]
    public async Task Ping_AnUnpairedNode_StoresNothing()
    {
        var node = UnpairedNode();
        HandshakeThrows(DaemonFailure.NotPaired);

        var ex = await Assert.ThrowsAsync<DaemonException>(() => _service.PingAsync(node.Guid));

        Assert.Equal(DaemonFailure.NotPaired, ex.Failure);
        _nodes.Verify(n => n.SaveDaemonStateAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task Ping_Cancelled_StoresNothing()
    {
        var node = PairedNode();
        _daemons.Setup(d => d.HandshakeAsync(node, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.PingAsync(node.Guid));

        _nodes.Verify(n => n.SaveDaemonStateAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task Ping_PassesTheCancellationToken()
    {
        var node = PairedNode();
        using var cts = new CancellationTokenSource();
        _daemons.Setup(d => d.HandshakeAsync(node, cts.Token))
            .ReturnsAsync(new DaemonHandshake(node.DaemonUuid!.Value, "0.1.0", "2.0.0", ProtocolCompatibility.Full));

        await _service.PingAsync(node.Guid, cts.Token);

        _daemons.Verify(d => d.HandshakeAsync(node, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Ping_AnUnknownNode_Throws()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.PingAsync(Guid.NewGuid()));
    }

    // ---------- health ----------

    [Fact]
    public async Task Health_ReturnsTheDaemonsAnswerAndStoresNothing()
    {
        var node = PairedNode();
        var health = new DaemonHealth(12.5, 1_000, 4_000, 87, TimeSpan.FromHours(3));
        _daemons.Setup(d => d.GetHealthAsync(node, It.IsAny<CancellationToken>())).ReturnsAsync(health);

        var result = await _service.GetHealthAsync(node.Guid);

        Assert.Same(health, result);
        _nodes.Verify(n => n.SaveDaemonStateAsync(It.IsAny<Node>()), Times.Never);
        _nodes.Verify(n => n.SavePairingAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task Health_AnUnknownNode_Throws()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetHealthAsync(Guid.NewGuid()));
    }
}
