using HedgehogPanel.UserManagment;
using HedgehogPanel.Servers;

namespace HedgehogPanel.Core.Managers;

/// <summary>
/// Defines methods for managing user accounts and authentication.
/// </summary>
public interface IAccountManager
{
    /// <summary>
    /// Authenticates a user with the specified username and password.
    /// </summary>
    /// <param name="username">The username of the account to be authenticated.</param>
    /// <param name="password">The password of the account to be authenticated.</param>
    /// <returns>
    /// An <see cref="Account"/> object representing the authenticated user if the authentication is successful.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the provided username or password is incorrect.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the account is locked or another issue prevents authentication.
    /// </exception>
    Task<Account> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Retrieves an account by the specified username.
    /// </summary>
    /// <param name="username">The username of the account to be retrieved.</param>
    /// <returns>
    /// An <see cref="Account"/> object representing the user associated with the specified username,
    /// or <c>null</c> if no such account exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided username is <c>null</c> or an empty string.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an error occurs during the retrieval process.
    /// </exception>
    Task<Account?> GetAccountByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user account with the specified details.
    /// </summary>
    /// <param name="username">The desired username for the new account.</param>
    /// <param name="email">The email address associated with the account.</param>
    /// <param name="password">The password for the new account.</param>
    /// <param name="firstName">The first name of the user (optional).</param>
    /// <param name="middleName">The middle name of the user (optional).</param>
    /// <param name="lastName">The last name of the user (optional).</param>
    /// <returns>
    /// An <see cref="Account"/> object representing the newly created user account.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided username, email, or password is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an account with the same username or email already exists.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when there is an error during account creation.
    /// </exception>
    Task<Account> CreateAccountAsync(string username, string email, string password, string? firstName = null, string? middleName = null, string? lastName = null);

    /// <summary>
    /// Updates the details of an existing user account, such as email, name, password, or other metadata.
    /// </summary>
    /// <param name="username">The username of the account to be updated.</param>
    /// <param name="newEmail">The new email address to associate with the account.</param>
    /// <param name="firstName">The updated first name of the user, if applicable.</param>
    /// <param name="middleName">The updated middle name of the user, if applicable.</param>
    /// <param name="lastName">The updated last name of the user, if applicable.</param>
    /// <param name="newPassword">The new password for the account, if applicable.</param>
    /// <param name="ip">The IP address of the requestor, used for logging or auditing purposes. Defaults to "unknown" if not provided.</param>
    /// <param name="actorGuid">The unique identifier of the user or system acting on this account update, used for auditing purposes.</param>
    /// <returns>
    /// A boolean value indicating whether the update operation was successful.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a database connection cannot be established or the connection type is invalid.
    /// </exception>
    Task<bool> UpdateAccountAsync(string username, string newEmail, string? firstName = null, string? middleName = null, string? lastName = null, string? newPassword = null, string? ip = "unknown", Guid? actorGuid = null, uint? expectedVersion = null);

    /// <summary>
    /// Deletes a user account with the specified username.
    /// </summary>
    /// <param name="username">The username of the account to be deleted.</param>
    /// <returns>
    /// A boolean value indicating whether the account was successfully deleted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database connection is not of the expected type.
    /// </exception>
    Task<bool> DeleteAccountAsync(string username);

    /// <summary>
    /// Retrieves a paginated list of user accounts.
    /// </summary>
    /// <param name="limit">The maximum number of accounts to retrieve. Defaults to 100.</param>
    /// <param name="offset">The number of accounts to skip before starting to retrieve. Defaults to 0.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a read-only list of <see cref="Account"/> objects
    /// representing the retrieved user accounts.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database connection type is invalid or another issue prevents account retrieval.
    /// </exception>
    Task<IReadOnlyList<Account>> ListAccountsAsync(int limit = 100, int offset = 0);

    /// <summary>
    /// Retrieves the list of servers associated with a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose server list is to be fetched.</param>
    /// <returns>
    /// A <see cref="List{Server}"/> containing the servers owned by the user or accessible to the user.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database connection is not of the expected type.
    /// </exception>
    Task<List<Server>> GetServerListAsync(Guid userId);
}
