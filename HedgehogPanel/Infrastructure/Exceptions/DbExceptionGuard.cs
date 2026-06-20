using System;
using System.Threading.Tasks;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Exceptions;

/// <summary>
/// Runs a database operation and translates Npgsql-specific exceptions into Hedgehog's typed
/// database exceptions, so Npgsql types never leak out of the Infrastructure layer.
/// </summary>
internal static class DbExceptionGuard
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DatabaseConstraintException(ex.ConstraintName ?? ex.SqlState, ex);
        }
        catch (PostgresException ex)
        {
            throw new DatabaseException($"Database query failed (SQL state {ex.SqlState}).", ex);
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseException("A database error occurred.", ex);
        }
    }
}
