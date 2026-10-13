using HedgehogPanel.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

/// <summary>
/// The application with the daemon replaced by a mock, so the pairing endpoints can be
/// driven through every answer a daemon can give without running one.
/// </summary>
public class FakeDaemonWebApplicationFactory : HedgehogWebApplicationFactory
{
    public Mock<INodeDaemonService> Daemon { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton(Daemon.Object));
    }
}
