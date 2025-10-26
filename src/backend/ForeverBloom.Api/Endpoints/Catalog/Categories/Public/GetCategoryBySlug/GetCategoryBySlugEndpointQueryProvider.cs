using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryBySlug;

public sealed class GetCategoryBySlugEndpointQueryProvider : IGetCategoryBySlugEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetCategoryBySlugEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SlugLookupResult?> GetSlugLookupAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugEntry = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.Slug == slug && s.EntityType == EntityType.Category)
            .FirstOrDefaultAsync(cancellationToken);

        if (slugEntry is null)
        {
            return null;
        }

        // If the provided slug is active, we already have the current slug
        if (slugEntry.IsActive)
        {
            return new SlugLookupResult
            {
                CategoryId = slugEntry.EntityId,
                CurrentSlug = slug,
                IsProvidedSlugCurrent = true
            };
        }

        // If the provided slug is inactive, find the current active slug for this category
        var currentSlugEntry = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.EntityId == slugEntry.EntityId &&
                       s.EntityType == EntityType.Category &&
                       s.IsActive)
            .FirstAsync(cancellationToken); // Use First() - there MUST be an active slug

        return new SlugLookupResult
        {
            CategoryId = slugEntry.EntityId,
            CurrentSlug = currentSlugEntry.Slug,
            IsProvidedSlugCurrent = false
        };
    }

    public async Task<GetCategoryBySlugResponse?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId && c.IsActive &&
                       !_dbContext.Categories.Any(ancestor =>
                           c.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive))
            .Select(category => new GetCategoryBySlugResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.CurrentSlug,
                ImagePath = category.ImagePath,
                ParentCategoryId = category.ParentCategoryId,
                Breadcrumbs = _dbContext.Categories
                    .Where(c => category.Path.IsDescendantOf(c.Path) && c.IsActive)
                    .OrderBy(c => c.Path)
                    .Select(c => new BreadcrumbItem
                    {
                        Name = c.Name,
                        Slug = c.CurrentSlug
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
