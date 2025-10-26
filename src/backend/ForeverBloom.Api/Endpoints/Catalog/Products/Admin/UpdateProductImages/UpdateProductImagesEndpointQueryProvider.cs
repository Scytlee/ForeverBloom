using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public sealed class UpdateProductImagesEndpointQueryProvider : IUpdateProductImagesEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateProductImagesEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }
}
