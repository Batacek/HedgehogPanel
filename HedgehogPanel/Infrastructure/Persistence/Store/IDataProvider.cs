using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Infrastructure.Persistence.Store;

/// <summary>
/// Represents a contract for implementing operations to retrieve and manage accounts and server-related data.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Retrieves the account information for a specific username asynchronously.
    /// </summary>
    /// <param name="username">The username of the account to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="Account"/> object if found; otherwise, null.</returns>
    Task<Account?> GetAccountByUsernameAsync(string username);

    /// <summary>
    /// Retrieves a list of servers associated with a specific user asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose servers are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a read-only list of <see cref="Server"/> objects associated with the user.</returns>
    Task<IReadOnlyList<Server>> GetServersByUserIdAsync(Guid userId);

    /// <summary>
    /// Executes a warmup operation for a specified user to prepare and pre-load the necessary data into the cache asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom the warmup operation is performed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WarmupAsync(Guid userId);

    /// <summary>
    /// Invalidates the cached account details for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose account cache is to be invalidated.</param>
    void InvalidateAccount(Guid userId);

    /// <summary>
    /// Invalidates the cached data for a specific server by its identifier.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server to invalidate.</param>
    void InvalidateServer(Guid serverId);
}
