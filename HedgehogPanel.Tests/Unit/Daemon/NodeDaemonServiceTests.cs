using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Hedgehog.V1;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Daemon;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Daemon;

public class NodeDaemonServiceTests
{
    private const int TimeoutSeconds = 5;
    private const int RetryCount = 2;

    private readonly Guid _panelUuid = Guid.NewGuid();
    private readonly Mock<IDaemonGrpcClient> _client = new();
    private readonly List<string> _addresses = new();
    private readonly Mock<IDaemonGrpcClientFactory> _factory = new();

    public NodeDaemonServiceTests()
    {
        _factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Callback<string>(address => _addresses.Add(address))
            .Returns(_client.Object);
    }

    private NodeDaemonService Service(int retryCount = RetryCount)
    {
        var identity = new Mock<IPanelIdentityProvider>();
        identity.Setup(i => i.PanelUuid).Returns(_panelUuid);
        var config = new HedgehogConfig
        {
            Daemon = new DaemonConfig
            {
                RequestTimeoutSeconds = TimeoutSeconds,
                RetryCount = retryCount,
                RetryDelayMilliseconds = 0
            }
        };
        return new NodeDaemonService(_factory.Object, identity.Object, config);
    }

    private static Node UnpairedNode(string ip = "10.0.0.1", int port = 50051) =>
        new(Guid.NewGuid(), "node", ip, port);

    private static Node PairedNode(Guid? daemonUuid = null) =>
        new(Guid.NewGuid(), "node", "10.0.0.1", 50051, daemonUuid: daemonUuid ?? Guid.NewGuid(), daemonToken: "token");

    // Fully qualified: the protocol has its own Status enum in Hedgehog.V1.
    private static RpcException Rpc(StatusCode status) => new(new Grpc.Core.Status(status, "detail"));

    private void ProbeReturns(string protocolVersion, long uptime = 42) =>
        _client.Setup(c => c.PublicHealthCheckAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicHealthCheckResponse { ProtocolVersion = protocolVersion, Uptime = uptime });

    private void ProbeThrows(StatusCode status) =>
        _client.Setup(c => c.PublicHealthCheckAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Rpc(status));

    // ---------- addresses and deadlines ----------

    [Fact]
    public async Task Calls_UsePlainHttpToTheNodesAddress()
    {
        ProbeReturns("2.0.0");

        await Service().ProbeAsync(UnpairedNode("10.0.0.1", 50051));

        Assert.Equal(new[] { "http://10.0.0.1:50051" }, _addresses);
    }

    [Fact]
    public void AddressOf_BracketsIPv6Literals()
    {
        Assert.Equal("http://[::1]:50051", NodeDaemonService.AddressOf(UnpairedNode("::1")));
        Assert.Equal("http://daemon.example:50051", NodeDaemonService.AddressOf(UnpairedNode("daemon.example")));
    }

    [Fact]
    public async Task Calls_CarryTheConfiguredDeadline()
    {
        DateTime? deadline = null;
        _client.Setup(c => c.PublicHealthCheckAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime?, CancellationToken>((d, _) => deadline = d)
            .ReturnsAsync(new PublicHealthCheckResponse { ProtocolVersion = "2.0.0" });

        var before = DateTime.UtcNow;
        await Service().ProbeAsync(UnpairedNode());
        var after = DateTime.UtcNow;

        Assert.NotNull(deadline);
        Assert.InRange(deadline.Value, before.AddSeconds(TimeoutSeconds), after.AddSeconds(TimeoutSeconds));
    }

    [Fact]
    public async Task InvalidAddress_IsReportedAsSuch()
    {
        _factory.Setup(f => f.CreateClient(It.IsAny<string>())).Throws(new UriFormatException("bad"));

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().ProbeAsync(UnpairedNode("not a host")));

        Assert.Equal(DaemonFailure.InvalidAddress, ex.Failure);
    }

    // ---------- retries ----------

    [Fact]
    public async Task TransientFailures_AreRetried()
    {
        _client.SetupSequence(c => c.PublicHealthCheckAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Rpc(StatusCode.Unavailable))
            .ThrowsAsync(Rpc(StatusCode.DeadlineExceeded))
            .ReturnsAsync(new PublicHealthCheckResponse { ProtocolVersion = "2.0.0" });

        var probe = await Service().ProbeAsync(UnpairedNode());

        Assert.Equal(ProtocolCompatibility.Full, probe.Compatibility);
        Assert.Equal(3, _addresses.Count);
    }

    [Fact]
    public async Task TransientFailures_GiveUpAfterTheRetryCount()
    {
        ProbeThrows(StatusCode.Unavailable);

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().ProbeAsync(UnpairedNode()));

        Assert.Equal(DaemonFailure.Unreachable, ex.Failure);
        Assert.Equal(1 + RetryCount, _addresses.Count);
    }

    [Fact]
    public async Task PermanentFailures_AreNotRetried()
    {
        ProbeThrows(StatusCode.Unauthenticated);

        await Assert.ThrowsAsync<DaemonException>(() => Service().ProbeAsync(UnpairedNode()));

        Assert.Single(_addresses);
    }

    [Fact]
    public async Task EveryAttempt_DisposesItsConnection()
    {
        ProbeThrows(StatusCode.Unavailable);

        await Assert.ThrowsAsync<DaemonException>(() => Service().ProbeAsync(UnpairedNode()));

        _client.Verify(c => c.DisposeAsync(), Times.Exactly(1 + RetryCount));
    }

    [Theory]
    [InlineData(StatusCode.Unavailable, DaemonFailure.Unreachable)]
    [InlineData(StatusCode.DeadlineExceeded, DaemonFailure.Timeout)]
    [InlineData(StatusCode.Unauthenticated, DaemonFailure.Unauthenticated)]
    [InlineData(StatusCode.PermissionDenied, DaemonFailure.PermissionDenied)]
    [InlineData(StatusCode.Unimplemented, DaemonFailure.NotSupported)]
    [InlineData(StatusCode.InvalidArgument, DaemonFailure.InvalidRequest)]
    [InlineData(StatusCode.Internal, DaemonFailure.Unexpected)]
    public async Task GrpcStatuses_AreTranslated(StatusCode status, DaemonFailure expected)
    {
        ProbeThrows(status);

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service(retryCount: 0).ProbeAsync(UnpairedNode()));

        Assert.Equal(expected, ex.Failure);
        Assert.IsType<RpcException>(ex.InnerException);
    }

    [Fact]
    public async Task Cancellation_SurfacesAsCancellationRatherThanAFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        ProbeThrows(StatusCode.Cancelled);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().ProbeAsync(UnpairedNode(), cts.Token));
    }

    // ---------- probe ----------

    [Fact]
    public async Task Probe_ReportsVersionCompatibilityAndUptime()
    {
        ProbeReturns("2.1.0", uptime: 90);

        var probe = await Service().ProbeAsync(UnpairedNode());

        Assert.Equal("2.1.0", probe.ProtocolVersion);
        Assert.Equal(ProtocolCompatibility.Limited, probe.Compatibility);
        Assert.Equal(TimeSpan.FromSeconds(90), probe.Uptime);
    }

    // ---------- register ----------

    private void RegisterReturns(RegisterPanelResponse response) =>
        _client.Setup(c => c.RegisterPanelAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

    [Fact]
    public async Task Register_SendsTheCodeAndThisPanelsIdentity()
    {
        var daemonUuid = Guid.NewGuid();
        RegisterReturns(new RegisterPanelResponse { Token = "issued", DaemonUuid = daemonUuid.ToString() });

        var pairing = await Service().RegisterAsync(UnpairedNode(), "ABCD-EFGH");

        Assert.Equal(new DaemonPairing(daemonUuid, "issued"), pairing);
        _client.Verify(c => c.RegisterPanelAsync("ABCD-EFGH", _panelUuid, It.Is<string>(n => n.StartsWith("Hedgehog Panel")),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_IsNeverRetried()
    {
        // A lost answer after the daemon accepted the code must not be followed by a retry.
        _client.Setup(c => c.RegisterPanelAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(Rpc(StatusCode.Unavailable));

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().RegisterAsync(UnpairedNode(), "ABCD-EFGH"));

        Assert.Equal(DaemonFailure.Unreachable, ex.Failure);
        Assert.Single(_addresses);
    }

    [Theory]
    [InlineData(ErrorCode.ErrorInvalidOneTimeCode, DaemonFailure.InvalidOneTimeCode)]
    [InlineData(ErrorCode.ErrorAlreadyRegistered, DaemonFailure.AlreadyRegistered)]
    public async Task Register_TranslatesRefusals(ErrorCode code, DaemonFailure expected)
    {
        RegisterReturns(new RegisterPanelResponse { ErrorCode = code });

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().RegisterAsync(UnpairedNode(), "ABCD-EFGH"));

        Assert.Equal(expected, ex.Failure);
    }

    [Fact]
    public async Task Register_WithoutATokenInTheAnswer_IsUnexpected()
    {
        RegisterReturns(new RegisterPanelResponse { Token = "", DaemonUuid = Guid.NewGuid().ToString() });

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().RegisterAsync(UnpairedNode(), "ABCD-EFGH"));

        Assert.Equal(DaemonFailure.Unexpected, ex.Failure);
    }

    // ---------- handshake ----------

    [Fact]
    public async Task Handshake_OnAnUnpairedNode_DoesNotCallTheDaemon()
    {
        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().HandshakeAsync(UnpairedNode()));

        Assert.Equal(DaemonFailure.NotPaired, ex.Failure);
        Assert.Empty(_addresses);
    }

    [Fact]
    public async Task Handshake_PresentsTheNodesTokenAndReportsCompatibility()
    {
        var daemonUuid = Guid.NewGuid();
        _client.Setup(c => c.HandshakeAsync(_panelUuid, "token", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HandshakeResponse { DaemonUuid = daemonUuid.ToString(), DaemonVersion = "0.1.0", ProtocolVersion = "2.0.0" });

        var handshake = await Service().HandshakeAsync(PairedNode(daemonUuid));

        Assert.Equal(new DaemonHandshake(daemonUuid, "0.1.0", "2.0.0", ProtocolCompatibility.Full), handshake);
    }

    [Fact]
    public async Task Handshake_WithADifferentDaemonAnswering_IsAnIdentityMismatch()
    {
        _client.Setup(c => c.HandshakeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HandshakeResponse { DaemonUuid = Guid.NewGuid().ToString(), ProtocolVersion = "2.0.0" });

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().HandshakeAsync(PairedNode()));

        Assert.Equal(DaemonFailure.IdentityMismatch, ex.Failure);
    }

    // ---------- health ----------

    [Fact]
    public async Task Health_MapsTheDaemonsReport()
    {
        var daemonUuid = Guid.NewGuid();
        _client.Setup(c => c.GetDetailedHealthAsync(_panelUuid, "token", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DetailedHealthResponse
            {
                CpuUsagePercent = 12.5f,
                RamUsedBytes = 1024,
                RamTotalBytes = 4096,
                RunningProcesses = 77,
                DaemonUuid = daemonUuid.ToString(),
                PublicHealthCheck = new PublicHealthCheckResponse { Uptime = 3600 }
            });

        var health = await Service().GetHealthAsync(PairedNode(daemonUuid));

        Assert.Equal(new DaemonHealth(12.5, 1024, 4096, 77, TimeSpan.FromHours(1)), health);
    }

    [Fact]
    public async Task Health_OnAnUnpairedNode_DoesNotCallTheDaemon()
    {
        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().GetHealthAsync(UnpairedNode()));

        Assert.Equal(DaemonFailure.NotPaired, ex.Failure);
        Assert.Empty(_addresses);
    }
}
