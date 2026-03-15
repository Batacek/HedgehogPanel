using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Persistence;
using Npgsql;

namespace HedgehogPanel.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// A factory class for creating PostgreSQL database connections using NpgsqlDataSource.
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public IDbConnection CreateConnection()
    {
        return _dataSource.CreateConnection();
    }

    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }
}
