using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public sealed class RestoreCategoryEndpointQueryProvider : IRestoreCategoryEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public RestoreCategoryEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .IgnoreQueryFilters()
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}
