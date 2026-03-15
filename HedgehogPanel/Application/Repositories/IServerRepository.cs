using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Repositories;

public interface IServerRepository
{
    /// Retrieves a server by its globally unique identifier (GUID).
    /// <param name="guid">The globally unique identifier of the server to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the server associated with the specified GUID, or null if no server is found.</returns>
    Task<Server?> GetByGuidAsync(Guid guid);

    /// Retrieves a list of servers with pagination support.
    /// <param name="limit">The maximum number of servers to retrieve.</param>
    /// <param name="offset">The number of servers to skip before starting to collect the result set.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a read-only list of servers within the specified range.</returns>
    Task<IReadOnlyList<Server>> ListAsync(int limit, int offset);

    /// Creates a new server in the repository.
    /// <param name="server">The server entity to create in the repository.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the server was successfully created.</returns>
    Task<bool> CreateAsync(Server server);

    /// Updates an existing server's details in the repository.
    /// <param name="server">The server entity containing the updated information to apply.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the update was successful.</returns>
    Task<bool> UpdateAsync(Server server);

    /// Deletes a server by its globally unique identifier (GUID).
    /// <param name="guid">The globally unique identifier of the server to delete.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the server was successfully deleted.</returns>
    Task<bool> DeleteAsync(Guid guid);
}
