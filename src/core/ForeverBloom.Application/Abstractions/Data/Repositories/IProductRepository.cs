using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.Application.Abstractions.Data.Repositories;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Retrieves a product by its ID, including archived products.
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, null otherwise</returns>
    Task<Product?> GetByIdIncludingArchivedAsync(long id, CancellationToken cancellationToken = default);
}
