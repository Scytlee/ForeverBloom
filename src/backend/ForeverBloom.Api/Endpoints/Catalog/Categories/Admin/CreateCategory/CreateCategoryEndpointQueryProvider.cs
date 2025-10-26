using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;

public sealed class CreateCategoryEndpointQueryProvider : ICreateCategoryEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public CreateCategoryEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugExists = await _dbContext.SlugRegistry
          .AsNoTracking()
          .AnyAsync(s => s.Slug == slug, cancellationToken);

        return !slugExists;
    }

    public Task<string?> GetCategoryPathByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
          .AsNoTracking()
          .Where(c => c.Id == categoryId)
          .Select(c => c.Path.ToString())
          .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CategoryNameExistsWithinParentAsync(string name, int? parentCategoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
          .AsNoTracking()
          .AnyAsync(c => c.Name == name && c.ParentCategoryId == parentCategoryId, cancellationToken);
    }

    public Task<int> GetParentHierarchyDepthAsync(int parentCategoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
          .AsNoTracking()
          .Where(c => c.Id == parentCategoryId)
          .Select(c => c.Path.NLevel)
          .FirstOrDefaultAsync(cancellationToken);
    }
}
