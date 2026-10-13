using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Repositories;

public interface INodeRepository
{
    /// <summary>
    /// Retrieves a node by its globally unique identifier (GUID).
    /// </summary>
    Task<Node?> GetByGuidAsync(Guid guid);

    /// <summary>
    /// Retrieves a list of nodes with pagination support.
    /// </summary>
    Task<IReadOnlyList<Node>> ListAsync(int limit, int offset);

    /// <summary>
    /// Creates a new node in the repository.
    /// </summary>
    Task<bool> CreateAsync(Node node);

    /// <summary>
    /// Updates an existing node's details in the repository.
    /// </summary>
    Task<bool> UpdateAsync(Node node);

    /// <summary>
    /// Stores the daemon identity and token from pairing, together with the daemon state.
    /// Leaves the fields an operator edits alone.
    /// </summary>
    Task<bool> SavePairingAsync(Node node);

    /// <summary>
    /// Stores the outcome of contacting a paired daemon: status, versions, last error and
    /// when it was last seen. Only applies while the node still holds the token that was
    /// used, so a result that arrives after the node was paired again is dropped.
    /// Returns false when nothing was stored.
    /// </summary>
    Task<bool> SaveDaemonStateAsync(Node node);

    /// <summary>
    /// Deletes a node by its globally unique identifier (GUID).
    /// </summary>
    Task<bool> DeleteAsync(Guid guid);
}
