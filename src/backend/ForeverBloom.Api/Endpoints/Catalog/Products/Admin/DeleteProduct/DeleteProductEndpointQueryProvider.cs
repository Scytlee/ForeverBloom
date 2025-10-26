using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.DeleteProduct;

public sealed class DeleteProductEndpointQueryProvider : IDeleteProductEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteProductEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<bool> ProductIsArchivedAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == productId && p.DeletedAt != null, cancellationToken);
    }
}
