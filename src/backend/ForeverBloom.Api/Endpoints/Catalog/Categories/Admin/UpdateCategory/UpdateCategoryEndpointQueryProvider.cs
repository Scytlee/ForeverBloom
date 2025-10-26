using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public sealed class UpdateCategoryEndpointQueryProvider : IUpdateCategoryEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateCategoryEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        // Tracking required, entity is being updated
        return _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> CategoryNameExistsAtSameLevelAsync(string name, int? parentCategoryId, int excludeCategoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Name == name && c.ParentCategoryId == parentCategoryId && c.Id != excludeCategoryId, cancellationToken);
    }

    public Task<bool> CategoryHasChildrenAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters() // Include soft-deleted and inactive categories
            .AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken);
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, int excludeCategoryId, CancellationToken cancellationToken = default)
    {
        // Check if slug is reserved by any entity other than the current category
        var slugIsReserved = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(e => !(e.EntityType == EntityType.Category && e.EntityId == excludeCategoryId))
            .AnyAsync(e => e.Slug == slug, cancellationToken);

        return !slugIsReserved;
    }

    public Task<string?> GetCategoryPathAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => c.Path.ToString())
            .FirstOrDefaultAsync(cancellationToken);
    }
}
