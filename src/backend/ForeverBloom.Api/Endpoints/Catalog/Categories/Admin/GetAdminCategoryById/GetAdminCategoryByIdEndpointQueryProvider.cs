using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.GetAdminCategoryById;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;

public sealed class GetAdminCategoryByIdEndpointQueryProvider : IGetAdminCategoryByIdEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetAdminCategoryByIdEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetAdminCategoryByIdResponse?> GetCategoryByIdAsync(int categoryId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking();

        // Handle archived items
        if (includeArchived)
        {
            query = query.IgnoreQueryFilters();
        }

        var response = await query
            .Where(c => c.Id == categoryId)
            .Select(c => new GetAdminCategoryByIdResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Slug = c.CurrentSlug,
                ImagePath = c.ImagePath,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                Path = c.Path.ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                DeletedAt = c.DeletedAt,
                RowVersion = c.RowVersion
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response;
    }
}
