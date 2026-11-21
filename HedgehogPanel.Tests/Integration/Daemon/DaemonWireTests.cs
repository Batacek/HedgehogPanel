using System;
using System.Diagnostics;
using System.Linq;
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

namespace HedgehogPanel.Tests.Integration.Daemon;

/// <summary>
/// The panel's daemon client against a daemon that really answers over HTTP/2. The unit tests
/// mock the client and the endpoint tests mock the service; this is the layer between them:
/// what goes on the wire, and how each answer is read.
/// </summary>
public class DaemonWireTests : IAsyncLifetime
{
    private const string Token = "token-of-this-node";

    private readonly Guid _panelUuid = Guid.NewGuid();
    private FakeDaemonServer _server = null!;

    public async Task InitializeAsync() => _server = await FakeDaemonServer.StartAsync();

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private FakeDaemon Daemon => _server.Daemon;

    private NodeDaemonService Service(int timeoutSeconds = 7, int retryCount = 2)
    {
        var config = new HedgehogConfig
        {
            Daemon = new DaemonConfig
            {
                RequestTimeoutSeconds = timeoutSeconds,
                RetryCount = retryCount,
                RetryDelayMilliseconds = 10
            }
        };
        var identity = new Mock<IPanelIdentityProvider>();
        identity.Setup(i => i.PanelUuid).Returns(_panelUuid);
        return new NodeDaemonService(new DaemonGrpcClientFactory(config), identity.Object, config);
    }

    private Node Unpaired(int? port = null) =>
        new(Guid.NewGuid(), "node", "127.0.0.1", port ?? _server.Port);

    private Node Paired(Guid? daemonUuid = null, int? port = null) =>
        new(Guid.NewGuid(), "node", "127.0.0.1", port ?? _server.Port, status: NodeStatus.Online,
            daemonUuid: daemonUuid ?? Daemon.DaemonUuid, daemonVersion: "0.1.0", protocolVersion: "2.0.0", daemonToken: Token);

    private static async Task<DaemonFailure> FailureOf(Func<Task> call)
    {
        var ex = await Assert.ThrowsAsync<DaemonException>(call);
        return ex.Failure;
    }

    // ---------- what goes on the wire ----------

    [Fact]
    public async Task Probe_SendsNoCredentialsAndReadsTheVersionAndUptime()
    {
        var probe = await Service().ProbeAsync(Unpaired());

        var call = Assert.Single(Daemon.Calls);
        Assert.Equal("PublicHealthCheck", call.Method);
        Assert.Null(call.Authorization);
        Assert.Null(call.PanelUuid);
        Assert.Equal("2.0.0", probe.ProtocolVersion);
        Assert.Equal(ProtocolCompatibility.Full, probe.Compatibility);
        Assert.Equal(TimeSpan.FromSeconds(90), probe.Uptime);
    }

    [Fact]
    public async Task Register_SendsTheCodeAndThePanelIdentityInTheBodyAndNoCredentials()
    {
        var pairing = await Service().RegisterAsync(Unpaired(), "ABCD-EFGH");

        var call = Assert.Single(Daemon.Calls);
        Assert.Equal("RegisterPanel", call.Method);
        Assert.Null(call.Authorization);
        var request = Assert.IsType<RegisterPanelRequest>(call.Request);
        Assert.Equal("ABCD-EFGH", request.OneTimeCode);
        Assert.Equal(_panelUuid.ToString(), request.PanelUuid);
        Assert.StartsWith("Hedgehog Panel (", request.PanelDisplayName);
        Assert.Equal("issued-token", pairing.Token);
        Assert.Equal(Daemon.DaemonUuid, pairing.DaemonUuid);
    }

    [Fact]
    public async Task Handshake_CarriesTheTokenAndThePanelUuidAsMetadata()
    {
        var handshake = await Service().HandshakeAsync(Paired());

        var call = Assert.Single(Daemon.Calls);
        Assert.Equal("Handshake", call.Method);
        Assert.Equal($"Bearer {Token}", call.Authorization);
        Assert.Equal(_panelUuid.ToString(), call.PanelUuid);
        Assert.Equal(Daemon.DaemonUuid, handshake.DaemonUuid);
        Assert.Equal("0.1.0", handshake.DaemonVersion);
        Assert.Equal("2.0.0", handshake.ProtocolVersion);
        Assert.Equal(ProtocolCompatibility.Full, handshake.Compatibility);
    }

    [Fact]
    public async Task Health_CarriesTheTokenAndThePanelUuidAndMapsTheLoad()
    {
        var health = await Service().GetHealthAsync(Paired());

        var call = Assert.Single(Daemon.Calls);
        Assert.Equal("DetailedHealth", call.Method);
        Assert.Equal($"Bearer {Token}", call.Authorization);
        Assert.Equal(_panelUuid.ToString(), call.PanelUuid);
        Assert.Equal(12.5f, health.CpuUsagePercent);
        Assert.Equal(2_000, health.RamUsedBytes);
        Assert.Equal(8_000, health.RamTotalBytes);
        Assert.Equal(42, health.RunningProcesses);
        Assert.Equal(TimeSpan.FromSeconds(3_600), health.Uptime);
    }

    [Fact]
    public async Task Calls_CarryADeadlineMatchingTheConfiguredTimeout()
    {
        await Service(timeoutSeconds: 7).HandshakeAsync(Paired());

        var call = Assert.Single(Daemon.Calls);
        Assert.NotNull(call.Deadline);
        var allowed = call.Deadline!.Value - call.ReceivedAt;
        Assert.InRange(allowed.TotalSeconds, 4, 7.5);
    }

    // ---------- gRPC statuses ----------

    [Theory]
    [InlineData(StatusCode.Unauthenticated, DaemonFailure.Unauthenticated)]
    [InlineData(StatusCode.PermissionDenied, DaemonFailure.PermissionDenied)]
    [InlineData(StatusCode.Unimplemented, DaemonFailure.NotSupported)]
    [InlineData(StatusCode.InvalidArgument, DaemonFailure.InvalidRequest)]
    [InlineData(StatusCode.Internal, DaemonFailure.Unexpected)]
    public async Task AStatusFromTheDaemon_IsTranslatedAndNotRetried(StatusCode status, DaemonFailure expected)
    {
        Daemon.OnHandshake = (_, _) => throw new RpcException(new Grpc.Core.Status(status, "refused"));

        var failure = await FailureOf(() => Service().HandshakeAsync(Paired()));

        Assert.Equal(expected, failure);
        Assert.Equal(1, Daemon.CallsTo("Handshake"));
    }

    [Fact]
    public async Task AStatusFromTheDaemon_KeepsItsDetailInTheMessage()
    {
        Daemon.OnDetailedHealth = (_, _) => throw new RpcException(new Grpc.Core.Status(StatusCode.PermissionDenied, "restricted mode"));

        var ex = await Assert.ThrowsAsync<DaemonException>(() => Service().GetHealthAsync(Paired()));

        Assert.Equal(DaemonFailure.PermissionDenied, ex.Failure);
        Assert.Contains("restricted mode", ex.Message);
    }

    [Fact]
    public async Task Unavailable_IsRetriedUntilTheAttemptsRunOut()
    {
        Daemon.OnHandshake = (_, _) => throw new RpcException(new Grpc.Core.Status(StatusCode.Unavailable, "busy"));

        var failure = await FailureOf(() => Service(retryCount: 2).HandshakeAsync(Paired()));

        Assert.Equal(DaemonFailure.Unreachable, failure);
        Assert.Equal(3, Daemon.CallsTo("Handshake"));
    }

    [Fact]
    public async Task Unavailable_ThenAnswering_Succeeds()
    {
        var attempts = 0;
        var fine = Daemon.OnHandshake;
        Daemon.OnHandshake = (request, context) =>
            Interlocked.Increment(ref attempts) <= 2
                ? throw new RpcException(new Grpc.Core.Status(StatusCode.Unavailable, "starting"))
                : fine(request, context);

        var handshake = await Service(retryCount: 2).HandshakeAsync(Paired());

        Assert.Equal(Daemon.DaemonUuid, handshake.DaemonUuid);
        Assert.Equal(3, Daemon.CallsTo("Handshake"));
    }

    [Fact]
    public async Task ARegistration_IsNeverRetried()
    {
        Daemon.OnRegisterPanel = (_, _) => throw new RpcException(new Grpc.Core.Status(StatusCode.Unavailable, "lost"));

        var failure = await FailureOf(() => Service(retryCount: 3).RegisterAsync(Unpaired(), "ABCD-EFGH"));

        Assert.Equal(DaemonFailure.Unreachable, failure);
        Assert.Equal(1, Daemon.CallsTo("RegisterPanel"));
    }

    [Fact]
    public async Task ADaemonThatDoesNotAnswerInTime_IsATimeout()
    {
        Daemon.OnHandshake = async (_, context) =>
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
            return new HandshakeResponse();
        };

        var watch = Stopwatch.StartNew();
        var failure = await FailureOf(() => Service(timeoutSeconds: 1, retryCount: 0).HandshakeAsync(Paired()));

        Assert.Equal(DaemonFailure.Timeout, failure);
        Assert.InRange(watch.Elapsed.TotalSeconds, 0.8, 5);
    }

    [Fact]
    public async Task ATimeout_IsRetried()
    {
        var attempts = 0;
        var fine = Daemon.OnHandshake;
        Daemon.OnHandshake = async (request, context) =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                await Task.Delay(Timeout.Infinite, context.CancellationToken);
            }

            return await fine(request, context);
        };

        var handshake = await Service(timeoutSeconds: 1, retryCount: 1).HandshakeAsync(Paired());

        Assert.Equal(Daemon.DaemonUuid, handshake.DaemonUuid);
        Assert.Equal(2, Daemon.CallsTo("Handshake"));
    }

    [Fact]
    public async Task ADaemonThatIsNotListening_IsUnreachable()
    {
        var port = _server.Port;
        await _server.DisposeAsync();

        var failure = await FailureOf(() => Service(retryCount: 1).HandshakeAsync(Paired(port: port)));

        Assert.Equal(DaemonFailure.Unreachable, failure);
    }

    [Fact]
    public async Task CancellingWhileTheDaemonThinks_CancelsTheCall()
    {
        var started = new TaskCompletionSource();
        Daemon.OnHandshake = async (_, context) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
            return new HandshakeResponse();
        };
        using var source = new CancellationTokenSource();

        var call = Service().HandshakeAsync(Paired(), source.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
    }

    // ---------- error codes in the body ----------

    [Theory]
    [InlineData(ErrorCode.ErrorInvalidOneTimeCode, DaemonFailure.InvalidOneTimeCode)]
    [InlineData(ErrorCode.ErrorAlreadyRegistered, DaemonFailure.AlreadyRegistered)]
    [InlineData((ErrorCode)99, DaemonFailure.Unexpected)]
    public async Task ARegistrationErrorCode_IsTranslated(ErrorCode code, DaemonFailure expected)
    {
        Daemon.OnRegisterPanel = (_, _) => Task.FromResult(new RegisterPanelResponse { ErrorCode = code });

        var failure = await FailureOf(() => Service().RegisterAsync(Unpaired(), "ABCD-EFGH"));

        Assert.Equal(expected, failure);
    }

    [Fact]
    public async Task ARegistrationWithoutATokenOrIdentity_IsRefused()
    {
        Daemon.OnRegisterPanel = (_, _) => Task.FromResult(new RegisterPanelResponse { ErrorCode = ErrorCode.ErrorNone });

        var failure = await FailureOf(() => Service().RegisterAsync(Unpaired(), "ABCD-EFGH"));

        Assert.Equal(DaemonFailure.Unexpected, failure);
    }

    [Fact]
    public async Task AHandshakeWithAnErrorCode_IsRefused()
    {
        Daemon.OnHandshake = (_, _) => Task.FromResult(new HandshakeResponse
        {
            DaemonUuid = Daemon.DaemonUuid.ToString(),
            ErrorCode = ErrorCode.ErrorAlreadyRegistered
        });

        var failure = await FailureOf(() => Service().HandshakeAsync(Paired()));

        Assert.Equal(DaemonFailure.Unexpected, failure);
    }

    // ---------- identity ----------

    [Fact]
    public async Task AnotherDaemonAnsweringAtTheAddress_IsRecognisedOnHandshakeAndHealth()
    {
        var other = Guid.NewGuid();
        var node = Paired(daemonUuid: other);

        Assert.Equal(DaemonFailure.IdentityMismatch, await FailureOf(() => Service().HandshakeAsync(node)));
        Assert.Equal(DaemonFailure.IdentityMismatch, await FailureOf(() => Service().GetHealthAsync(node)));
    }

    [Fact]
    public async Task ANodeNeverPaired_IsNotContactedForCallsThatNeedAToken()
    {
        var node = Unpaired();

        Assert.Equal(DaemonFailure.NotPaired, await FailureOf(() => Service().HandshakeAsync(node)));
        Assert.Equal(DaemonFailure.NotPaired, await FailureOf(() => Service().GetHealthAsync(node)));
        Assert.Empty(Daemon.Calls);
    }

    [Fact]
    public async Task EveryAuthenticatedCall_CarriesThePanelUuid()
    {
        var service = Service();
        var node = Paired();

        await service.HandshakeAsync(node);
        await service.HandshakeAsync(node);
        await service.GetHealthAsync(node);

        Assert.Equal(3, Daemon.Calls.Count);
        Assert.All(Daemon.Calls, c => Assert.Equal(_panelUuid.ToString(), c.PanelUuid));
    }
}
