using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProduct;

public sealed class UpdateProductEndpointQueryProvider : IUpdateProductEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateProductEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        // Entity needs to be tracked for updates - don't use AsNoTracking
        return _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AnyAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, int excludeProductId, CancellationToken cancellationToken = default)
    {
        // Check if slug is reserved by any entity other than the current product
        var slugIsReserved = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(e => !(e.EntityType == EntityType.Product && e.EntityId == excludeProductId))
            .AnyAsync(e => e.Slug == slug, cancellationToken);

        return !slugIsReserved;
    }

    public Task<CategoryInfo?> GetCategoryInfoAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new CategoryInfo
            {
                Name = c.Name,
                CurrentSlug = c.CurrentSlug
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

}
