using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ForeverBloom.Persistence.Data;

/// <summary>
/// Factory for creating PostgreSQL database connections using Npgsql.
/// </summary>
internal sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' not found.");
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
