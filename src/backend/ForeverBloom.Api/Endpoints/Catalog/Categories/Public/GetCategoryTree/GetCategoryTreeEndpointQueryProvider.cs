using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryTree;

public sealed class GetCategoryTreeEndpointQueryProvider : IGetCategoryTreeEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public GetCategoryTreeEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetCategoryTreeResponse> GetCategoryTreeAsync(int? rootCategoryId = null, int? depth = null, CancellationToken cancellationToken = default)
    {
        // ARCHITECTURAL DECISION: Active children of inactive parents are excluded

        var relevantCategories = await GetRelevantCategoriesAsync(rootCategoryId, depth, cancellationToken);
        var categoryLookup = relevantCategories.ToLookup(c => c.ParentCategoryId);

        // Determine starting categories based on rootCategoryId
        var startingCategories = rootCategoryId.HasValue
            ? relevantCategories.Where(c => c.Id == rootCategoryId.Value)
            : categoryLookup[null];

        var treeCategories = BuildTree(startingCategories, categoryLookup);

        return new GetCategoryTreeResponse(treeCategories);
    }

    private async Task<List<Category>> GetRelevantCategoriesAsync(int? rootCategoryId, int? depth, CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive &&
                       !_dbContext.Categories.Any(ancestor =>
                           c.Path.IsDescendantOf(ancestor.Path) && !ancestor.IsActive));

        if (rootCategoryId.HasValue)
        {
            var rootCategories = _dbContext.Categories
                .Where(c => c.Id == rootCategoryId.Value && c.IsActive);

            // Use ltree to get descendants of the root category
            query = query.Where(c =>
                rootCategories.Any(root => c.Path.IsDescendantOf(root.Path) || c.Path == root.Path));

            // Apply depth limit if specified
            if (depth.HasValue)
            {
                query = query.Where(c =>
                    rootCategories.Any(root => c.Path.NLevel <= root.Path.NLevel + depth.Value));
            }
        }
        else
        {
            // No rootCategoryId specified, but apply depth limit if specified
            if (depth.HasValue)
            {
                query = query.Where(c => c.Path.NLevel <= depth.Value);
            }
        }

        return await query
            .OrderBy(c => c.Path)
            .ThenBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    private static List<CategoryTreeItem> BuildTree(
        IEnumerable<Category> categories,
        ILookup<int?, Category> categoryLookup)
    {
        return categories
            .Select(category => new CategoryTreeItem(
                Id: category.Id,
                Name: category.Name,
                Slug: category.CurrentSlug,
                ImagePath: category.ImagePath,
                Children: BuildTree(categoryLookup[category.Id], categoryLookup)
            ))
            .ToList();
    }
}
