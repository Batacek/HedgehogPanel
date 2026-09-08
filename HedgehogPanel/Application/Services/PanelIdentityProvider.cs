using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Infrastructure.Logging;
using Microsoft.Extensions.Hosting;

namespace HedgehogPanel.Application.Services;

/// <summary>
/// Resolves the panel's identity once at startup and holds it for the process.
/// </summary>
/// <remarks>
/// Runs as a hosted service so an unreadable or corrupted identity fails the
/// application on boot rather than on the first attempt to reach a daemon.
/// </remarks>
public class PanelIdentityProvider : IPanelIdentityProvider, IHostedService
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(PanelIdentityProvider));

    private readonly IPanelIdentityStore _store;
    private Guid? _panelUuid;

    public PanelIdentityProvider(IPanelIdentityStore store)
    {
        _store = store;
    }

    public Guid PanelUuid => _panelUuid ?? throw new InvalidOperationException(
        "The panel UUID has not been loaded yet. It is resolved during startup, so reading it " +
        "earlier than that means something is running before the host has started.");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var identity = _store.LoadOrCreate();
        _panelUuid = identity.Uuid;

        if (identity.Created)
        {
            Logger.Information(
                "Generated new panel UUID: {PanelUuid}. Daemons paired with a previous identity " +
                "will reject this panel until they are paired again.",
                identity.Uuid);
        }
        else
        {
            Logger.Information("Panel UUID: {PanelUuid}", identity.Uuid);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
