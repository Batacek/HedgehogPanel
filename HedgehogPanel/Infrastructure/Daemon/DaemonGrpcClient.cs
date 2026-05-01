using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Hedgehog.V1;

namespace HedgehogPanel.Infrastructure.Daemon;

public class DaemonGrpcClient : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DaemonService.DaemonServiceClient _client;

    public DaemonGrpcClient(DaemonService.DaemonServiceClient client, GrpcChannel channel)
    {
        _client = client;
        _channel = channel;
    }

    public async Task<DetailedHealthResponse> GetDetailedHealthAsync(Guid panelUuid, string token)
    {
        var request = new DetailedHealthRequest
        {
            Auth = new AuthContext
            {
                PanelUuid = panelUuid.ToString(),
                Token = token
            }
        };
        return await _client.DetailedHealthAsync(request);
    }

    public async Task<HandshakeResponse> HandshakeAsync(Guid panelUuid, string token)
    {
        var request = new HandshakeRequest
        {
            Auth = new AuthContext
            {
                PanelUuid = panelUuid.ToString(),
                Token = token
            }
        };
        return await _client.HandshakeAsync(request);
    }

    public async Task<PublicHealthCheckResponse> PublicHealthCheckAsync()
    {
        var request = new PublicHealthCheckRequest();
        return await _client.PublicHealthCheckAsync(request);
    }

    public async Task<RegisterPanelResponse> RegisterPanelAsync(string oneTimeCode, Guid panelUuid, string panelDisplayName)
    {
        var request = new RegisterPanelRequest
        {
            OneTimeCode = oneTimeCode,
            PanelUuid = panelUuid.ToString(),
            PanelDisplayName = panelDisplayName
        };
        return await _client.RegisterPanelAsync(request);
    }

    public async Task<StartServerResponse> StartServerAsync(Guid panelUuid, string token, Guid serverUuid)
    {
        var request = new StartServerRequest
        {
            Auth = new AuthContext
            {
                PanelUuid = panelUuid.ToString(),
                Token = token
            },
            ServerUuid = serverUuid.ToString()
        };
        return await _client.StartServerAsync(request);
    }

    public async Task<StopServerResponse> StopServerAsync(Guid panelUuid, string token, Guid serverUuid)
    {
        var request = new StopServerRequest
        {
            Auth = new AuthContext
            {
                PanelUuid = panelUuid.ToString(),
                Token = token
            },
            ServerUuid = serverUuid.ToString()
        };
        return await _client.StopServerAsync(request);
    }

    public async Task<GetServerStatusResponse> GetServerStatusAsync(Guid panelUuid, string token, Guid serverUuid)
    {
        var request = new GetServerStatusRequest
        {
            Auth = new AuthContext
            {
                PanelUuid = panelUuid.ToString(),
                Token = token
            },
            ServerUuid = serverUuid.ToString()
        };
        return await _client.GetServerStatusAsync(request);
    }

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
