using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Application.Services;

/// <summary>A node as stored after talking to its daemon, and how well the daemon's protocol fits this panel.</summary>
public record NodeConnection(Node Node, ProtocolCompatibility Compatibility);

/// <summary>
/// Pairs nodes with their daemons and checks on them, storing the outcome on the node.
/// Failures surface as <see cref="HedgehogPanel.Application.Exceptions.DaemonException"/>;
/// a node that does not exist as <see cref="HedgehogPanel.Domain.Exceptions.EntityNotFoundException"/>.
/// </summary>
public interface INodeConnectionService
{
    /// <summary>
    /// Pairs the node with its daemon using the one-time code the daemon printed. A daemon
    /// whose protocol cannot work with this panel is refused before the code is spent.
    /// Pairing a node that is already paired replaces its token.
    /// </summary>
    Task<NodeConnection> PairAsync(Guid nodeId, string oneTimeCode);

    /// <summary>
    /// Handshakes with the node's daemon and stores what it reported. A failure is stored
    /// on the node as well before it is thrown.
    /// </summary>
    Task<NodeConnection> PingAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>Reads the load on the node's host. Stores nothing.</summary>
    Task<DaemonHealth> GetHealthAsync(Guid nodeId, CancellationToken cancellationToken = default);
}
