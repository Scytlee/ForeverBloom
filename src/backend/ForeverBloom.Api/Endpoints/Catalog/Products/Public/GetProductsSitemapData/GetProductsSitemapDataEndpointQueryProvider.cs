using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductsSitemapData;

public sealed class GetProductsSitemapDataEndpointQueryProvider : IGetProductsSitemapDataEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetProductsSitemapDataEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProductSitemapDataItem>> GetProductsSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.PublishStatus == PublishStatus.Published)
            .Where(p => p.Category.IsActive == true)
            .Where(p => !_dbContext.Categories.Any(ancestor =>
                p.Category.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive))
            .Select(p => new ProductSitemapDataItem
            {
                Slug = p.CurrentSlug,
                UpdatedOn = DateOnly.FromDateTime(p.UpdatedAt.UtcDateTime)
            })
            .ToListAsync(cancellationToken);
    }
}
