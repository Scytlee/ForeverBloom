using ForeverBloom.Api.Contracts.Catalog.Products.Admin.GetAdminProductById;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.GetAdminProductById;

public sealed class GetAdminProductByIdEndpointQueryProvider : IGetAdminProductByIdEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetAdminProductByIdEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetAdminProductByIdResponse?> GetProductByIdAsync(int productId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();

        // Handle archived items
        if (includeArchived)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.Id == productId)
            .Select(p => new GetAdminProductByIdResponse
            {
                Id = p.Id,
                Name = p.Name,
                SeoTitle = p.SeoTitle,
                FullDescription = p.FullDescription,
                MetaDescription = p.MetaDescription,
                Slug = p.CurrentSlug,
                Price = p.Price,
                DisplayOrder = p.DisplayOrder,
                IsFeatured = p.IsFeatured,
                PublishStatus = p.PublishStatus,
                Availability = p.Availability,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CategorySlug = p.Category.CurrentSlug,
                Images = p.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new AdminProductImageItem
                    {
                        ImagePath = i.ImagePath,
                        IsPrimary = i.IsPrimary,
                        DisplayOrder = i.DisplayOrder,
                        AltText = i.AltText
                    })
                    .ToList(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DeletedAt = p.DeletedAt,
                RowVersion = p.RowVersion
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
