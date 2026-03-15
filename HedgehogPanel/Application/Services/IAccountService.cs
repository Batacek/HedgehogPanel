using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Services;

public interface IAccountService
{
    /// <summary>
    /// Authenticates a user based on the provided username and password.
    /// </summary>
    /// <param name="username">The username of the account to authenticate.</param>
    /// <param name="password">The password of the account to authenticate.</param>
    /// <returns>
    /// The authenticated <see cref="Account"/> if the credentials are valid; otherwise, <c>null</c>.
    /// </returns>
    Task<Account?> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Retrieves an account by its username asynchronously.
    /// </summary>
    /// <param name="username">The username of the account to retrieve.</param>
    /// <returns>
    /// The <see cref="Account"/> associated with the specified username if found; otherwise, <c>null</c>.
    /// </returns>
    Task<Account?> GetAccountByUsernameAsync(string username);

    /// <summary>
    /// Retrieves an account by its unique identifier asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the account to retrieve.</param>
    /// <returns>
    /// The <see cref="Account"/> associated with the specified unique identifier if found; otherwise, <c>null</c>.
    /// </returns>
    Task<Account?> GetAccountByIdAsync(Guid userId);

    /// <summary>
    /// Creates a new user account with the provided information.
    /// </summary>
    /// <param name="username">The desired username for the new account.</param>
    /// <param name="email">The email address associated with the new account.</param>
    /// <param name="password">The password for the new account.</param>
    /// <param name="firstName">The first name of the account holder. Optional.</param>
    /// <param name="middleName">The middle name of the account holder. Optional.</param>
    /// <param name="lastName">The last name of the account holder. Optional.</param>
    /// <returns>
    /// The created <see cref="Account"/> instance containing account details.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when the account creation process fails.
    /// </exception>
    Task<Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null);

    /// <summary>
    /// Updates the details of an existing account asynchronously.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account to update.</param>
    /// <param name="newDetails">The object containing the updated account details.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a value indicating whether the update
    Task<bool> UpdateAccountAsync(Account account, string? newPassword = null);

    /// <summary>
    /// Deletes an account associated with the specified username asynchronously.
    /// </summary>
    /// <param name="username">The username of the account to be deleted.</param>
    /// <returns>
    /// A <c>bool</c> value indicating whether the account was successfully deleted.
    /// <c>true</c> if the account was deleted; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> DeleteAccountAsync(string username);

    /// <summary>
    /// Retrieves a list of accounts with pagination support.
    /// </summary>
    /// <param name="limit">The maximum number of accounts to retrieve. Defaults to 100.</param>
    /// <param name="offset">The number of accounts to skip before starting to retrieve the list. Defaults to 0.</param>
    /// <returns>
    /// A read-only list of <see cref="Account"/> objects within the specified range.
    /// </returns>
    Task<IReadOnlyList<Account>> ListAccountsAsync(int limit = 100, int offset = 0);
}
