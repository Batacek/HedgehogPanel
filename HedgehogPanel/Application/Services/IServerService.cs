using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Services;

public interface IServerService
{
    /// Retrieves a server by its globally unique identifier (GUID).
    /// <param name="id">The globally unique identifier of the server to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the server associated with the specified GUID, or null if no server is found.</returns>
    Task<Server?> GetServerByIdAsync(Guid id);

    /// Retrieves a list of servers with optional pagination.
    /// <param name="limit">The maximum number of servers to retrieve. Defaults to 100.</param>
    /// <param name="offset">The number of servers to skip before starting to retrieve. Defaults to 0.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a read-only list of servers.</returns>
    Task<IReadOnlyList<Server>> ListServersAsync(int limit = 100, int offset = 0);

    /// Creates a new server with the specified name, hostname, and port.
    /// <param name="name">The name of the server to create. This value cannot be null or empty.</param>
    /// <param name="hostname">The hostname of the server to create. This value cannot be null or empty.</param>
    /// <param name="port">The port number for the server. Defaults to 22 if not provided.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the newly created server instance.</returns>
    Task<Server> CreateServerAsync(string name, string hostname, int port = 22);

    /// Updates the details of an existing server.
    /// <param name="server">The server entity containing updated information to be saved.</param>
    /// <returns>A task representing the asynchronous operation. The task result is a boolean indicating whether the update operation succeeded.</returns>
    Task<bool> UpdateServerAsync(Server server);

    /// Deletes a server identified by its globally unique identifier (GUID).
    /// <param name="id">The globally unique identifier of the server to delete.</param>
    /// <returns>A task representing the asynchronous operation. The task result is a boolean indicating whether the delete operation succeeded.</returns>
    Task<bool> DeleteServerAsync(Guid id);
    /// <summary>
    /// Retrieves a list of servers owned by a specific user.
    /// </summary>
    Task<IReadOnlyList<Server>> ListServersByOwnerAsync(Guid userGuid, int limit = 100, int offset = 0);

    /// <summary>
    /// Retrieves a list of servers that have no owner.
    /// </summary>
    Task<IReadOnlyList<Server>> ListUnownedServersAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Retrieves the username of the owner of a server.
    /// </summary>
    Task<string?> GetServerOwnerUsernameAsync(Guid serverGuid);

    /// <summary>
    /// Assigns a server to a specific user.
    /// </summary>
    Task<bool> AssignServerToUserAsync(Guid serverGuid, Guid userGuid);
}
