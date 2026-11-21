using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Logging;
using Microsoft.Extensions.Hosting;

namespace HedgehogPanel.Application.Services;

/// <summary>
/// Checks on every paired daemon at a fixed pause, so a node's status and last-seen time
/// stay current without anyone pinging it by hand.
/// </summary>
/// <remarks>
/// Each check is <see cref="INodeConnectionService.PingAsync"/>, which already stores its
/// outcome and drops a result that arrives after the node was paired again. A round ends
/// before the pause begins, so rounds never overlap, however slow a daemon is.
/// </remarks>
public class NodeHealthPoller : BackgroundService
{
    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(NodeHealthPoller));

    private readonly INodeRepository _nodes;
    private readonly INodeConnectionService _connections;
    private readonly DaemonConfig _config;

    public NodeHealthPoller(INodeRepository nodes, INodeConnectionService connections, HedgehogConfig config)
    {
        _nodes = nodes;
        _connections = connections;
        _config = config.Daemon;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.PollingEnabled)
        {
            Logger.Information("Polling of paired daemons is switched off.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _config.PollIntervalSeconds));
        Logger.Information("Checking paired daemons every {IntervalSeconds} seconds.", (int)interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Typically the database being unreachable. The next round tries again.
                Logger.Error(ex, "A round of daemon checks failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Checks every paired node once, a few at a time.</summary>
    public async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var nodes = await _nodes.ListPairedAsync();
        if (nodes.Count == 0) return;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, _config.PollMaxParallelism),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(nodes, options, async (node, ct) => await CheckAsync(node, ct));
    }

    private async Task CheckAsync(Node node, CancellationToken cancellationToken)
    {
        try
        {
            await _connections.PingAsync(node.Guid, cancellationToken);
        }
        catch (DaemonException ex)
        {
            // Already stored on the node; an offline daemon is an expected state, not a fault here.
            Logger.Debug("Node {NodeId} did not check out: {Failure}.", node.Guid, ex.Failure);
        }
        catch (EntityNotFoundException)
        {
            Logger.Debug("Node {NodeId} was deleted during the round.", node.Guid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // One node must not stop the others from being checked.
            Logger.Warning(ex, "Checking node {NodeId} failed unexpectedly.", node.Guid);
        }
    }
}
