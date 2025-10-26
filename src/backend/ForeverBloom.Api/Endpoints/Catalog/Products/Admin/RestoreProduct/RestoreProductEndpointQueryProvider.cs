using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.RestoreProduct;

public sealed class RestoreProductEndpointQueryProvider : IRestoreProductEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public RestoreProductEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId, cancellationToken);
    }
}
