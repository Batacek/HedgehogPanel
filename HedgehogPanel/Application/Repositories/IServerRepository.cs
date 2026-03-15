using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Repositories;

public interface IServerRepository
{
    /// <summary>
    /// Retrieves a server by its globally unique identifier (GUID).
    /// </summary>
    Task<Server?> GetByGuidAsync(Guid guid);

    /// <summary>
    /// Retrieves a list of servers with pagination support.
    /// </summary>
    Task<IReadOnlyList<Server>> ListAsync(int limit, int offset);

    /// <summary>
    /// Creates a new server in the repository.
    /// </summary>
    Task<bool> CreateAsync(Server server);

    /// <summary>
    /// Updates an existing server's details in the repository.
    /// </summary>
    Task<bool> UpdateAsync(Server server);

    /// <summary>
    /// Deletes a server by its globally unique identifier (GUID).
    /// </summary>
    Task<bool> DeleteAsync(Guid guid);

    /// <summary>
    /// Retrieves a list of servers owned by a specific user.
    /// </summary>
    Task<IReadOnlyList<Server>> ListByOwnerAsync(Guid userGuid, int limit = 100, int offset = 0);

    /// <summary>
    /// Retrieves a list of servers that have no owner.
    /// </summary>
    Task<IReadOnlyList<Server>> ListUnownedAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Retrieves the username of the owner of a server.
    /// </summary>
    Task<string?> GetOwnerUsernameAsync(Guid serverGuid);

    /// <summary>
    /// Assigns a server to a specific user.
    /// </summary>
    Task<bool> AssignToUserAsync(Guid serverGuid, Guid userGuid);
}
