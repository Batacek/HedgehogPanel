using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HedgehogPanel.Core.Managers;

/// <summary>
/// Defines methods for managing servers, including listing, creating, and deleting servers.
/// </summary>
public interface IServerManager
{
    /// <summary>
    /// Retrieves a list of servers with optional offset and limit parameters for pagination.
    /// </summary>
    /// <param name="limit">The maximum number of servers to retrieve. Defaults to 100.</param>
    /// <param name="offset">The starting index for retrieval. Defaults to 0.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of server items.</returns>
    Task<IReadOnlyList<ServerManager.ServerListItem>> ListServersAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Creates a new server with the specified name, description, and optional owner user GUID.
    /// </summary>
    /// <param name="name">The name of the server. This parameter is required and cannot be null or whitespace.</param>
    /// <param name="description">An optional description of the server. If null, the description will be empty.</param>
    /// <param name="ownerUserGuid">An optional GUID representing the owner of the server. If null, the server will not have an assigned owner.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a server list item representing the created server.</returns>
    Task<ServerManager.ServerListItem> CreateServerAsync(string name, string? description = null, Guid? ownerUserGuid = null);

    /// <summary>
    /// Deletes a server identified by its unique identifier.
    /// </summary>
    /// <param name="id">The globally unique identifier (GUID) of the server to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the server was successfully deleted.</returns>
    Task<bool> DeleteServerAsync(Guid id);
}
