using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Application.Services;

/// <summary>What an unauthenticated probe learns about a daemon before pairing.</summary>
public record DaemonProbe(string ProtocolVersion, ProtocolCompatibility Compatibility, TimeSpan Uptime);

/// <summary>The result of pairing: the daemon's identity and the token every later call presents.</summary>
public record DaemonPairing(Guid DaemonUuid, string Token);

/// <summary>What an authenticated handshake confirms about a paired daemon.</summary>
public record DaemonHandshake(Guid DaemonUuid, string DaemonVersion, string ProtocolVersion, ProtocolCompatibility Compatibility);

/// <summary>A snapshot of the load on a daemon's host.</summary>
public record DaemonHealth(double CpuUsagePercent, long RamUsedBytes, long RamTotalBytes, int RunningProcesses, TimeSpan Uptime);

/// <summary>
/// Talks to the daemon behind a node. Every failure surfaces as a
/// <see cref="HedgehogPanel.Application.Exceptions.DaemonException"/> whose
/// <see cref="HedgehogPanel.Application.Exceptions.DaemonFailure"/> says what went wrong,
/// never as a raw transport error.
/// </summary>
public interface INodeDaemonService
{
    /// <summary>Unauthenticated: reads the daemon's protocol version and judges compatibility.</summary>
    Task<DaemonProbe> ProbeAsync(Node node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pairs this panel with the node's daemon using a one-time code. Never retried,
    /// because the code can only be used once.
    /// </summary>
    Task<DaemonPairing> RegisterAsync(Node node, string oneTimeCode, CancellationToken cancellationToken = default);

    /// <summary>Confirms the daemon still accepts this panel and is the one the node was paired with.</summary>
    Task<DaemonHandshake> HandshakeAsync(Node node, CancellationToken cancellationToken = default);

    /// <summary>Reads CPU, memory and process counts from the daemon's host.</summary>
    Task<DaemonHealth> GetHealthAsync(Node node, CancellationToken cancellationToken = default);
}
