using System;

namespace HedgehogPanel.Infrastructure.Daemon;

public interface IDaemonGrpcClientFactory
{
    DaemonGrpcClient CreateClient(string daemonAddress);
}
