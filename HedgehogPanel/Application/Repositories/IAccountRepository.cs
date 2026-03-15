using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Repositories;

public interface IAccountRepository
{
    /// <summary>
    /// Asynchronously retrieves an account by its unique identifier (GUID).
    /// </summary>
    /// <param name="guid">The GUID of the account to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the account corresponding to the provided GUID, or null if no account is found.</returns>
    Task<Account?> GetByGuidAsync(Guid guid);

    /// <summary>
    /// Asynchronously retrieves an account by its unique username.
    /// </summary>
    /// <param name="username">The username of the account to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the account corresponding to the provided username, or null if no account is found.</returns>
    Task<Account?> GetByUsernameAsync(string username);

    /// <summary>
    /// Asynchronously authenticates an account using the provided username and password.
    /// </summary>
    /// <param name="username">The username of the account to authenticate.</param>
    /// <param name="password">The password associated with the specified username.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the authenticated account if the credentials are valid, or null if authentication fails.</returns>
    Task<Account?> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Asynchronously retrieves a paginated list of accounts.
    /// </summary>
    /// <param name="limit">The maximum number of accounts to retrieve.</param>
    /// <param name="offset">The number of accounts to skip before starting to collect the result set.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a read-only list of accounts.</returns>
    Task<IReadOnlyList<Account>> ListAsync(int limit, int offset);

    /// <summary>
    /// Asynchronously creates a new account with the specified details and stores it in the repository.
    /// </summary>
    /// <param name="account">The account entity containing details of the new account to be created.</param>
    /// <param name="passwordHash">The pre-hashed password associated with the account.</param>
    /// <returns>A task representing the asynchronous operation. The result of the task is true if the account was successfully created; otherwise, false.</returns>
    Task<bool> CreateAsync(Account account, string passwordHash);

    /// <summary>
    /// Asynchronously updates an account with the provided details and optionally updates the password.
    /// </summary>
    /// <param name="account">The account object containing the updated details to persist.</param>
    /// <param name="newPasswordHash">An optional new password hash to update for the account. If null, the password remains unchanged.</param>
    /// <returns>A task representing the asynchronous operation. The task result is a boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateAsync(Account account, string? newPasswordHash = null);

    /// <summary>
    /// Asynchronously deletes an account by its unique identifier (GUID).
    /// </summary>
    /// <param name="guid">The GUID of the account to delete.</param>
    /// <returns>A task representing the asynchronous operation. The task result is true if the account was successfully deleted, or false if no account with the given GUID was found.</returns>
    Task<bool> DeleteAsync(Guid guid);
}
