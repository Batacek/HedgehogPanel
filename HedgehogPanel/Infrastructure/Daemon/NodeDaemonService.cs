using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Hedgehog.V1;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Logging;

namespace HedgehogPanel.Infrastructure.Daemon;

public class NodeDaemonService : INodeDaemonService
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(NodeDaemonService));

    /// <summary>How this panel introduces itself; the daemon shows it in its list of trusted panels.</summary>
    private static readonly string PanelDisplayName = $"Hedgehog Panel ({Environment.MachineName})";

    private readonly IDaemonGrpcClientFactory _clientFactory;
    private readonly IPanelIdentityProvider _identity;
    private readonly DaemonConfig _config;

    public NodeDaemonService(IDaemonGrpcClientFactory clientFactory, IPanelIdentityProvider identity, HedgehogConfig config)
    {
        _clientFactory = clientFactory;
        _identity = identity;
        _config = config.Daemon;
    }

    /// <summary>
    /// The daemon endpoint for a node. Plain HTTP/2 for now: the daemon has no TLS yet, so the
    /// connection is unencrypted and only safe on a trusted network. Change the scheme when
    /// daemon TLS lands.
    /// </summary>
    public static string AddressOf(Node node)
    {
        var host = IPAddress.TryParse(node.IpAddress, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{node.IpAddress}]"
            : node.IpAddress;
        return $"http://{host}:{node.Port}";
    }

    public Task<DaemonProbe> ProbeAsync(Node node, CancellationToken cancellationToken = default)
    {
        return CallAsync(node, retry: true, cancellationToken, async (client, deadline, ct) =>
        {
            var response = await client.PublicHealthCheckAsync(deadline, ct);
            return new DaemonProbe(
                response.ProtocolVersion,
                DaemonProtocol.Evaluate(response.ProtocolVersion),
                TimeSpan.FromSeconds(response.Uptime));
        });
    }

    public async Task<DaemonPairing> RegisterAsync(Node node, string oneTimeCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oneTimeCode);

        // Never retried: if the daemon accepted the code but its answer was lost, a retry would
        // present a spent code and fail, leaving a token issued that this panel never learned.
        var response = await CallAsync(node, retry: false, cancellationToken, (client, deadline, ct) =>
            client.RegisterPanelAsync(oneTimeCode, _identity.PanelUuid, PanelDisplayName, deadline, ct));

        switch (response.ErrorCode)
        {
            case ErrorCode.ErrorNone:
                break;
            case ErrorCode.ErrorInvalidOneTimeCode:
                throw new DaemonException(DaemonFailure.InvalidOneTimeCode,
                    $"Daemon at {AddressOf(node)} rejected the pairing code as wrong, expired or already used.");
            case ErrorCode.ErrorAlreadyRegistered:
                throw new DaemonException(DaemonFailure.AlreadyRegistered,
                    $"Daemon at {AddressOf(node)} says this panel is already registered.");
            default:
                throw new DaemonException(DaemonFailure.Unexpected,
                    $"Daemon at {AddressOf(node)} answered the pairing with unknown error code {response.ErrorCode}.");
        }

        if (string.IsNullOrWhiteSpace(response.Token) || !Guid.TryParse(response.DaemonUuid, out var daemonUuid))
        {
            throw new DaemonException(DaemonFailure.Unexpected,
                $"Daemon at {AddressOf(node)} accepted the pairing but returned no usable token or identity.");
        }

        return new DaemonPairing(daemonUuid, response.Token);
    }

    public async Task<DaemonHandshake> HandshakeAsync(Node node, CancellationToken cancellationToken = default)
    {
        var token = RequireToken(node);
        var response = await CallAsync(node, retry: true, cancellationToken, (client, deadline, ct) =>
            client.HandshakeAsync(_identity.PanelUuid, token, deadline, ct));

        if (response.ErrorCode != ErrorCode.ErrorNone || !Guid.TryParse(response.DaemonUuid, out var daemonUuid))
        {
            throw new DaemonException(DaemonFailure.Unexpected,
                $"Daemon at {AddressOf(node)} answered the handshake without a usable identity.");
        }

        EnsureSameDaemon(node, daemonUuid);

        return new DaemonHandshake(
            daemonUuid,
            response.DaemonVersion,
            response.ProtocolVersion,
            DaemonProtocol.Evaluate(response.ProtocolVersion));
    }

    public async Task<DaemonHealth> GetHealthAsync(Node node, CancellationToken cancellationToken = default)
    {
        var token = RequireToken(node);
        var response = await CallAsync(node, retry: true, cancellationToken, (client, deadline, ct) =>
            client.GetDetailedHealthAsync(_identity.PanelUuid, token, deadline, ct));

        if (Guid.TryParse(response.DaemonUuid, out var daemonUuid))
        {
            EnsureSameDaemon(node, daemonUuid);
        }

        return new DaemonHealth(
            response.CpuUsagePercent,
            response.RamUsedBytes,
            response.RamTotalBytes,
            response.RunningProcesses,
            TimeSpan.FromSeconds(response.PublicHealthCheck?.Uptime ?? 0));
    }

    /// <summary>
    /// Runs one call against the node's daemon with a deadline, retrying transient failures when
    /// <paramref name="retry"/> allows it, and translates anything that still fails.
    /// </summary>
    private async Task<T> CallAsync<T>(
        Node node,
        bool retry,
        CancellationToken cancellationToken,
        Func<IDaemonGrpcClient, DateTime, CancellationToken, Task<T>> call)
    {
        var address = AddressOf(node);
        var attempts = retry ? 1 + Math.Max(0, _config.RetryCount) : 1;

        for (var attempt = 1; ; attempt++)
        {
            RpcException? failure = null;

            await using (var client = CreateClient(address))
            {
                try
                {
                    var deadline = DateTime.UtcNow.AddSeconds(_config.RequestTimeoutSeconds);
                    return await call(client, deadline, cancellationToken);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("The call to the daemon was cancelled.", ex, cancellationToken);
                }
                catch (RpcException ex)
                {
                    failure = ex;
                }
            }

            if (attempt >= attempts || !IsTransient(failure.StatusCode))
            {
                throw Translate(failure, address);
            }

            Logger.Debug("Daemon at {Address} failed with {Status} on attempt {Attempt} of {Attempts}; retrying.",
                address, failure.StatusCode, attempt, attempts);
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, _config.RetryDelayMilliseconds) * attempt), cancellationToken);
        }
    }

    private IDaemonGrpcClient CreateClient(string address)
    {
        try
        {
            return _clientFactory.CreateClient(address);
        }
        catch (Exception ex) when (ex is UriFormatException or ArgumentException or InvalidOperationException)
        {
            throw new DaemonException(DaemonFailure.InvalidAddress, $"'{address}' is not a usable daemon address.", ex);
        }
    }

    private static bool IsTransient(StatusCode status) =>
        status is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

    private static DaemonException Translate(RpcException ex, string address)
    {
        var (failure, what) = ex.StatusCode switch
        {
            StatusCode.Unavailable => (DaemonFailure.Unreachable, "could not be reached"),
            StatusCode.DeadlineExceeded => (DaemonFailure.Timeout, "did not answer in time"),
            StatusCode.Unauthenticated => (DaemonFailure.Unauthenticated, "rejected this panel's credentials"),
            StatusCode.PermissionDenied => (DaemonFailure.PermissionDenied, "does not allow this operation"),
            StatusCode.Unimplemented => (DaemonFailure.NotSupported, "does not support this operation"),
            StatusCode.InvalidArgument => (DaemonFailure.InvalidRequest, "rejected the request as invalid"),
            _ => (DaemonFailure.Unexpected, $"failed with status {ex.StatusCode}")
        };

        return new DaemonException(failure, $"Daemon at {address} {what}: {ex.Status.Detail}", ex);
    }

    private static string RequireToken(Node node)
    {
        if (string.IsNullOrWhiteSpace(node.DaemonToken))
        {
            throw new DaemonException(DaemonFailure.NotPaired, $"Node '{node.Name}' is not paired with its daemon yet.");
        }

        return node.DaemonToken;
    }

    private static void EnsureSameDaemon(Node node, Guid answered)
    {
        if (node.DaemonUuid is { } expected && expected != answered)
        {
            throw new DaemonException(DaemonFailure.IdentityMismatch,
                $"Node '{node.Name}' was paired with daemon {expected}, but daemon {answered} answered at {AddressOf(node)}.");
        }
    }
}
