using System.Data;

namespace HedgehogPanel.Core.Database;

/// <summary>
/// Represents a factory for creating database connections.
/// This interface outlines the functionality for obtaining new database connection instances.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and returns a new database connection.
    /// </summary>
    /// <returns>An instance of IDbConnection.</returns>
    IDbConnection CreateConnection();


    /// <summary>
    /// Asynchronously creates and returns a new database connection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an instance of IDbConnection.</returns>
    ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
