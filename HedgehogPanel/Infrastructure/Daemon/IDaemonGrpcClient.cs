using System;
using System.Threading;
using System.Threading.Tasks;
using Hedgehog.V1;

namespace HedgehogPanel.Infrastructure.Daemon;

/// <summary>
/// One connection to one daemon. Every call takes a deadline and a cancellation token,
/// so a daemon that never answers cannot hold its caller indefinitely.
/// </summary>
public interface IDaemonGrpcClient : IAsyncDisposable
{
    Task<PublicHealthCheckResponse> PublicHealthCheckAsync(DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<RegisterPanelResponse> RegisterPanelAsync(string oneTimeCode, Guid panelUuid, string panelDisplayName, DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<HandshakeResponse> HandshakeAsync(Guid panelUuid, string token, DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<DetailedHealthResponse> GetDetailedHealthAsync(Guid panelUuid, string token, DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<StartServerResponse> StartServerAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<StopServerResponse> StopServerAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default);

    Task<GetServerStatusResponse> GetServerStatusAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default);
}
