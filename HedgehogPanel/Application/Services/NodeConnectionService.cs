using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Exceptions;
using HedgehogPanel.Infrastructure.Logging;

namespace HedgehogPanel.Application.Services;

public class NodeConnectionService : INodeConnectionService
{
    // Daemon codes are eight characters, shown as ABCD-EFGH. The limit only keeps junk away
    // from the daemon; the daemon decides whether a code is valid.
    private const int MaxCodeLength = 32;

    private static readonly ILoggerService Logger = HedgehogLogger.ForContext(typeof(NodeConnectionService));

    private readonly INodeRepository _nodes;
    private readonly INodeDaemonService _daemons;

    public NodeConnectionService(INodeRepository nodes, INodeDaemonService daemons)
    {
        _nodes = nodes;
        _daemons = daemons;
    }

    public async Task<NodeConnection> PairAsync(Guid nodeId, string oneTimeCode)
    {
        var code = oneTimeCode?.Trim();
        if (string.IsNullOrEmpty(code)) throw new ValidationException("A pairing code is required.");
        if (code.Length > MaxCodeLength) throw new ValidationException("The pairing code is too long.");

        var node = await LoadAsync(nodeId);

        // No cancellation token on purpose: once the daemon has accepted the code, the token
        // it returns has to be stored even if the caller has gone away. Every daemon call
        // still ends at its deadline.
        var probe = await _daemons.ProbeAsync(node);
        if (probe.Compatibility == ProtocolCompatibility.Incompatible)
        {
            throw new DaemonException(DaemonFailure.Incompatible,
                $"The daemon speaks protocol {probe.ProtocolVersion}, which cannot work with protocol {DaemonProtocol.Version} of this panel.");
        }

        var pairing = await _daemons.RegisterAsync(node, code);

        if (node.DaemonUuid is { } previous && previous != pairing.DaemonUuid)
        {
            Logger.Warning("Node {NodeId} was paired with daemon {PreviousDaemon} and is now paired with daemon {DaemonUuid}.",
                node.Guid, previous, pairing.DaemonUuid);
        }

        var paired = node.WithPairing(pairing.DaemonUuid, pairing.Token, probe.ProtocolVersion, DateTime.UtcNow);
        try
        {
            // The daemon has issued a token by now. If it cannot be stored, it is lost and
            // the daemon has to print a new code.
            if (!await _nodes.SavePairingAsync(paired))
            {
                Logger.Warning("Node {NodeId} was deleted while it was being paired; the daemon's token is lost.", nodeId);
                throw new EntityNotFoundException("Node", nodeId);
            }
        }
        catch (DatabaseConstraintException ex)
        {
            Logger.Warning(ex, "Daemon {DaemonUuid} is already paired as another node; node {NodeId} was not paired.",
                pairing.DaemonUuid, nodeId);
            throw new DuplicateEntityException("Node", "daemon", pairing.DaemonUuid);
        }

        Logger.Information("Node {NodeId} paired with daemon {DaemonUuid}, protocol {ProtocolVersion}.",
            nodeId, pairing.DaemonUuid, probe.ProtocolVersion);
        return new NodeConnection(paired, probe.Compatibility);
    }

    public async Task<NodeConnection> PingAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await LoadAsync(nodeId);

        try
        {
            var handshake = await _daemons.HandshakeAsync(node, cancellationToken);
            var status = handshake.Compatibility == ProtocolCompatibility.Incompatible
                ? NodeStatus.Incompatible
                : NodeStatus.Online;

            var contacted = node.WithContact(status, handshake.DaemonVersion, handshake.ProtocolVersion, DateTime.UtcNow);
            await StoreAsync(contacted);
            return new NodeConnection(contacted, handshake.Compatibility);
        }
        catch (DaemonException ex) when (ex.Failure != DaemonFailure.NotPaired)
        {
            // An unpaired node has nothing to record; every other failure says something
            // about the daemon and is kept on the node.
            await StoreAsync(node.WithFailure(StatusAfter(ex.Failure), ex.Message));
            throw;
        }
    }

    public async Task<DaemonHealth> GetHealthAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await LoadAsync(nodeId);
        return await _daemons.GetHealthAsync(node, cancellationToken);
    }

    private async Task<Node> LoadAsync(Guid nodeId) =>
        await _nodes.GetByGuidAsync(nodeId) ?? throw new EntityNotFoundException("Node", nodeId);

    private async Task StoreAsync(Node node)
    {
        if (!await _nodes.SaveDaemonStateAsync(node))
        {
            Logger.Debug("Daemon state of node {NodeId} was not stored: the node was paired again or deleted meanwhile.", node.Guid);
        }
    }

    /// <summary>What a failed contact says about the node.</summary>
    private static NodeStatus StatusAfter(DaemonFailure failure) => failure switch
    {
        DaemonFailure.Unreachable or DaemonFailure.Timeout or DaemonFailure.InvalidAddress => NodeStatus.Offline,
        DaemonFailure.Incompatible => NodeStatus.Incompatible,
        // The daemon answered but something is wrong with the pairing or the answer.
        _ => NodeStatus.Unknown
    };
}
