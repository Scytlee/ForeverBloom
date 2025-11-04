using ForeverBloom.Domain.Abstractions;

namespace ForeverBloom.Application.Abstractions.Data.Repositories;

/// <summary>
/// Base repository interface for common entity operations.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : Entity
{
    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    void Add(T entity);
}
