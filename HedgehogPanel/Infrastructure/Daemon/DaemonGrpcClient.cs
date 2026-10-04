using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Hedgehog.V1;

namespace HedgehogPanel.Infrastructure.Daemon;

public class DaemonGrpcClient : IDaemonGrpcClient
{
    private readonly GrpcChannel _channel;
    private readonly DaemonService.DaemonServiceClient _client;

    public DaemonGrpcClient(DaemonService.DaemonServiceClient client, GrpcChannel channel)
    {
        _client = client;
        _channel = channel;
    }

    /// <summary>
    /// Builds the metadata every authenticated call carries. Protocol 2.0.0 moved
    /// authentication out of the request bodies so the daemon can verify it in a
    /// single interceptor. gRPC requires metadata keys to be lowercase.
    /// </summary>
    private static Metadata AuthHeaders(Guid panelUuid, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new Metadata
        {
            { "authorization", $"Bearer {token}" },
            { "x-panel-uuid", panelUuid.ToString() }
        };
    }

    /// <summary>
    /// Unauthenticated. Doubles as the compatibility probe: the response carries the
    /// daemon's protocol version, so the panel can check it before trying to pair.
    /// </summary>
    public async Task<PublicHealthCheckResponse> PublicHealthCheckAsync(DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        var request = new PublicHealthCheckRequest();
        return await _client.PublicHealthCheckAsync(request, deadline: deadline, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Unauthenticated: the one-time code is the credential. Returns the token that
    /// every later call must present.
    /// </summary>
    public async Task<RegisterPanelResponse> RegisterPanelAsync(string oneTimeCode, Guid panelUuid, string panelDisplayName, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        var request = new RegisterPanelRequest
        {
            OneTimeCode = oneTimeCode,
            PanelUuid = panelUuid.ToString(),
            PanelDisplayName = panelDisplayName
        };
        return await _client.RegisterPanelAsync(request, deadline: deadline, cancellationToken: cancellationToken);
    }

    public async Task<HandshakeResponse> HandshakeAsync(Guid panelUuid, string token, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return await _client.HandshakeAsync(new HandshakeRequest(), AuthHeaders(panelUuid, token), deadline, cancellationToken);
    }

    public async Task<DetailedHealthResponse> GetDetailedHealthAsync(Guid panelUuid, string token, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return await _client.DetailedHealthAsync(new DetailedHealthRequest(), AuthHeaders(panelUuid, token), deadline, cancellationToken);
    }

    public async Task<StartServerResponse> StartServerAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        var request = new StartServerRequest { ServerUuid = serverUuid.ToString() };
        return await _client.StartServerAsync(request, AuthHeaders(panelUuid, token), deadline, cancellationToken);
    }

    public async Task<StopServerResponse> StopServerAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        var request = new StopServerRequest { ServerUuid = serverUuid.ToString() };
        return await _client.StopServerAsync(request, AuthHeaders(panelUuid, token), deadline, cancellationToken);
    }

    public async Task<GetServerStatusResponse> GetServerStatusAsync(Guid panelUuid, string token, Guid serverUuid, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        var request = new GetServerStatusRequest { ServerUuid = serverUuid.ToString() };
        return await _client.GetServerStatusAsync(request, AuthHeaders(panelUuid, token), deadline, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
