using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public sealed class DeleteCategoryEndpointQueryProvider : IDeleteCategoryEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteCategoryEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryDeletionValidationResult> ValidateCategoryForDeletionAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var validationResult = await _dbContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Id == categoryId)
            .Select(c => new CategoryDeletionValidationResult
            {
                Exists = true, // If we find a category, it exists
                IsArchived = c.DeletedAt != null,
                HasChildCategories = c.ChildCategories.Any(),
                HasProducts = c.Products.Any()
            })
            .FirstOrDefaultAsync(cancellationToken);

        // If FirstOrDefaultAsync returns null, the category doesn't exist at all
        return validationResult ?? new CategoryDeletionValidationResult { Exists = false };
    }
}
