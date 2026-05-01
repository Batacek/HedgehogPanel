using System.Net.Http;
using Grpc.Net.Client;
using Hedgehog.V1;
using HedgehogPanel.Infrastructure.Configuration;

namespace HedgehogPanel.Infrastructure.Daemon;

public class DaemonGrpcClientFactory : IDaemonGrpcClientFactory
{
    private readonly HedgehogConfig _config;

    public DaemonGrpcClientFactory(HedgehogConfig config)
    {
        _config = config;
    }

    public DaemonGrpcClient CreateClient(string daemonAddress)
    {
        var channelOptions = new GrpcChannelOptions();

        if (_config.Daemon.AllowInsecureConnections)
        {
            channelOptions.HttpHandler = new SocketsHttpHandler
            {
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };
        }

        var channel = GrpcChannel.ForAddress(daemonAddress, channelOptions);
        var client = new DaemonService.DaemonServiceClient(channel);
        return new DaemonGrpcClient(client, channel);
    }
}
