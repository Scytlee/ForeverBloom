using System.Data;

namespace ForeverBloom.Application.Abstractions.Data;

/// <summary>
/// Factory for creating database connections.
/// Abstracts connection creation logic from application handlers.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a database connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An opened database connection</returns>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
