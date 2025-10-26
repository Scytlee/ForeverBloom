using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductBySlug;

public sealed class GetProductBySlugEndpointQueryProvider : IGetProductBySlugEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetProductBySlugEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SlugLookupResult?> GetSlugLookupAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugEntry = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.Slug == slug && s.EntityType == EntityType.Product)
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
                ProductId = slugEntry.EntityId,
                CurrentSlug = slug,
                IsProvidedSlugCurrent = true
            };
        }

        // If the provided slug is inactive, find the current active slug for this product
        var currentSlugEntry = await _dbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.EntityId == slugEntry.EntityId &&
                       s.EntityType == EntityType.Product &&
                       s.IsActive)
            .FirstAsync(cancellationToken); // Use First() - there MUST be an active slug

        return new SlugLookupResult
        {
            ProductId = slugEntry.EntityId,
            CurrentSlug = currentSlugEntry.Slug,
            IsProvidedSlugCurrent = false
        };
    }

    public async Task<GetProductBySlugResponse?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.PublishStatus == PublishStatus.Published && p.Category.IsActive &&
                       !_dbContext.Categories.Any(ancestor =>
                           p.Category.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive))
            .Select(p => new GetProductBySlugResponse
            {
                Id = p.Id,
                Name = p.Name,
                SeoTitle = p.SeoTitle,
                FullDescription = p.FullDescription,
                MetaDescription = p.MetaDescription,
                Slug = p.CurrentSlug,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Availability = p.Availability,
                IsFeatured = p.IsFeatured,
                Images = p.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ProductImageItem
                    {
                        ImagePath = i.ImagePath,
                        IsPrimary = i.IsPrimary,
                        DisplayOrder = i.DisplayOrder,
                        AltText = i.AltText
                    })
                    .ToList()
            });

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
