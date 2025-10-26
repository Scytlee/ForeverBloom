using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public sealed class ArchiveCategoryEndpointQueryProvider : IArchiveCategoryEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public ArchiveCategoryEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> HasChildCategoriesAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken);
    }
}
