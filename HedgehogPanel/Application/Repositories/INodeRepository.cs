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
    /// Deletes a node by its globally unique identifier (GUID).
    /// </summary>
    Task<bool> DeleteAsync(Guid guid);
}
