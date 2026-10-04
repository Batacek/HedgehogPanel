using System;

namespace HedgehogPanel.Infrastructure.Daemon;

public interface IDaemonGrpcClientFactory
{
    IDaemonGrpcClient CreateClient(string daemonAddress);
}
