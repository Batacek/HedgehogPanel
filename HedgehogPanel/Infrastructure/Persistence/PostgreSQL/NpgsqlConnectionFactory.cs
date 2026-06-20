using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Infrastructure.Exceptions;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// A factory class for creating PostgreSQL database connections using NpgsqlDataSource.
/// Connection failures are translated into <see cref="DatabaseConnectionException"/> so callers
/// never see raw Npgsql exceptions.
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _host;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _host = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Host ?? "unknown";
    }

    public IDbConnection CreateConnection()
    {
        try
        {
            return _dataSource.CreateConnection();
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseConnectionException(_host, ex);
        }
    }

    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dataSource.OpenConnectionAsync(cancellationToken);
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseConnectionException(_host, ex);
        }
    }
}
