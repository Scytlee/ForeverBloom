using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Repositories;

internal sealed class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> ExistsAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Category>()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }

    public async Task<HierarchicalPath?> GetCategoryPathAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Category>()
            .Where(c => c.Id == categoryId)
            .Select(c => c.Path)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> NameExistsWithinParentAsync(
        SeoTitle name,
        long? parentCategoryId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Category>()
            .AsNoTracking()
            .AnyAsync(
                c => c.Name == name
                    && c.ParentCategoryId == parentCategoryId
                    && c.DeletedAt == null,
                cancellationToken);
    }

    [SuppressMessage("ReSharper", "EntityFramework.ClientSideDbFunctionCall")]
    public async Task<IReadOnlyList<Category>> GetDescendantsAsync(
        HierarchicalPath parentPath,
        long excludeCategoryId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var parentLTree = new LTree(parentPath.Value);

        return await DbContext.Set<Category>()
            .IgnoreQueryFilters()
            .Where(c => c.Id != excludeCategoryId
                     && EF.Property<LTree>(c, nameof(Category.Path)).IsDescendantOf(parentLTree))
            .OrderBy(c => EF.Property<LTree>(c, nameof(Category.Path)).NLevel)
            .Take(maxCount + 1)
            .ToListAsync(cancellationToken);
    }
}
