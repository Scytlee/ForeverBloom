using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoriesSitemapData;

public sealed class GetCategoriesSitemapDataEndpointQueryProvider : IGetCategoriesSitemapDataEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetCategoriesSitemapDataEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CategorySitemapDataItem>> GetCategoriesSitemapDataAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive == true)
            .Where(c => !_dbContext.Categories.Any(ancestor =>
                c.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive))
            .Select(c => new CategorySitemapDataItem
            {
                Slug = c.CurrentSlug,
                UpdatedOn = DateOnly.FromDateTime(c.UpdatedAt.UtcDateTime)
            })
             .ToListAsync(cancellationToken);
    }
}
