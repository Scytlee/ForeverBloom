using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Application.Abstractions.Data.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Checks if a category with the specified ID exists.
    /// </summary>
    /// <param name="categoryId">The category ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the category exists, false otherwise</returns>
    Task<bool> ExistsAsync(long categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves category's hierarchical path.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category's hierarchical path if category exists; otherwise null.</returns>
    Task<HierarchicalPath?> GetCategoryPathAsync(long categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a category with the provided name already exists under the specified parent.
    /// </summary>
    /// <param name="name">The category name to inspect.</param>
    /// <param name="parentCategoryId">The parent category identifier (null for root categories).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a sibling category with the same name exists; otherwise false.</returns>
    Task<bool> NameExistsWithinParentAsync(SeoTitle name, long? parentCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all descendants of a category, including soft-deleted ones.
    /// </summary>
    /// <param name="parentPath">The hierarchical path of the parent category.</param>
    /// <param name="excludeCategoryId">The category ID to exclude from results (typically the parent itself).</param>
    /// <param name="maxCount">Maximum number of descendants to retrieve (fetches maxCount + 1 for cap detection).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of descendant categories, ordered by path, including soft-deleted ones.</returns>
    Task<IReadOnlyList<Category>> GetDescendantsAsync(
        HierarchicalPath parentPath,
        long excludeCategoryId,
        int maxCount,
        CancellationToken cancellationToken = default);
}
