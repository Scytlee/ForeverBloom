using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ArchiveProduct;

public sealed class ArchiveProductEndpointQueryProvider : IArchiveProductEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public ArchiveProductEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId, cancellationToken);
    }
}
