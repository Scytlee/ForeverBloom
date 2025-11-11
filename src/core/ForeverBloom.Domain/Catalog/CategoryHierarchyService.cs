using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Domain service for category hierarchy operations.
/// </summary>
public sealed class CategoryHierarchyService
{
    /// <summary>
    /// Rebases a collection of categories by replacing the old base path prefix with a new base path prefix.
    /// This is the general primitive used by reparent and slug-change operations to update descendant paths.
    /// </summary>
    /// <param name="oldBase">The old base path prefix (subject's path before the change).</param>
    /// <param name="newBase">The new base path prefix (subject's path after the change).</param>
    /// <param name="categories">The categories to rebase.</param>
    /// <param name="timestamp">The timestamp of this operation.</param>
    /// <returns>
    /// A Result containing <c>true</c> if changes were made, <c>false</c> if no-op,
    /// or a failure with a domain error (e.g., depth exceeded, rebase validation failed).
    /// </returns>
    private Result<bool> RebaseCategories(
        HierarchicalPath oldBase,
        HierarchicalPath newBase,
        IReadOnlyList<Category> categories,
        DateTimeOffset timestamp)
    {
        // No-op: bases are identical or no categories were provided
        if (oldBase.Value == newBase.Value || categories.Count == 0)
        {
            return Result<bool>.Success(false);
        }

        // Validate max depth for all descendants
        var depthChange = newBase.Depth - oldBase.Depth;
        var currentMaxDepth = categories.Max(d => d.Path.Depth);
        var newMaxDepth = currentMaxDepth + depthChange;

        if (newMaxDepth > HierarchicalPath.MaxDepth)
        {
            return Result<bool>.Failure(new HierarchicalPathErrors.TooDeep(currentMaxDepth + depthChange));
        }

        // Rebase all descendants
        foreach (var descendant in categories)
        {
            var result = descendant.RebasePath(oldBase, newBase, timestamp);
            if (result.IsFailure)
            {
                return Result<bool>.Failure(result.Error);
            }
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Reparents a category to a new parent and rebases all its descendants to reflect the new hierarchy.
    /// </summary>
    /// <param name="category">The category to reparent.</param>
    /// <param name="newParentId">The new parent category ID (null for root categories).</param>
    /// <param name="newParentPath">The new parent's hierarchical path (null for root categories).</param>
    /// <param name="descendants">The descendants of the category being reparented.</param>
    /// <param name="timestamp">The timestamp of this operation.</param>
    /// <returns>
    /// A Result containing <c>true</c> if changes were made, <c>false</c> if no-op,
    /// or a failure with a domain error (e.g., circular dependency, depth exceeded).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when newParentId and newParentPath are not consistent (one null, one not).
    /// </exception>
    public Result<bool> ReparentCategoryAndRebaseDescendants(
        Category category,
        long? newParentId,
        HierarchicalPath? newParentPath,
        IReadOnlyList<Category> descendants,
        DateTimeOffset timestamp)
    {
        // Guard: newParentId and newParentPath must both be provided or null
        if ((newParentId is null && newParentPath is not null)
            || (newParentId is not null && newParentPath is null))
        {
            throw new ArgumentException(
                "newParentId and newParentPath must both be provided or null.");
        }

        var oldBase = category.Path;

        // New parent must not be the subject itself or any of its descendants
        if (newParentPath is not null && newParentPath.IsDescendantOf(oldBase, includeSelf: true))
        {
            return Result<bool>.Failure(
                new CategoryErrors.CircularDependency(category.Id, newParentId!.Value));
        }

        var reparentResult = category.Reparent(newParentId, newParentPath, timestamp);
        if (reparentResult.IsFailure)
        {
            return Result<bool>.Failure(reparentResult.Error);
        }

        // No-op: category didn't change
        if (reparentResult.Value is false)
        {
            return Result<bool>.Success(false);
        }

        var newBase = category.Path;

        var rebaseResult = RebaseCategories(oldBase, newBase, descendants, timestamp);
        return rebaseResult.IsSuccess switch
        {
            true => Result<bool>.Success(true),
            false => rebaseResult
        };
    }

    /// <summary>
    /// Changes a category's slug and rebases all its descendants to reflect the new path.
    /// </summary>
    /// <param name="category">The category whose slug is being changed.</param>
    /// <param name="newSlug">The new slug to apply.</param>
    /// <param name="descendants">The descendants of the category whose slug is being changed.</param>
    /// <param name="timestamp">The timestamp of this operation.</param>
    /// <returns>
    /// A Result containing <c>true</c> if changes were made, <c>false</c> if no-op,
    /// or a failure with a domain error (e.g., depth exceeded, invalid slug).
    /// </returns>
    public Result<bool> ChangeCategorySlugAndRebaseDescendants(
        Category category,
        Slug newSlug,
        IReadOnlyList<Category> descendants,
        DateTimeOffset timestamp)
    {
        // Early no-op detection
        if (category.CurrentSlug == newSlug)
        {
            return Result<bool>.Success(false);
        }

        var oldBase = category.Path;

        var changeSlugResult = category.ChangeSlug(newSlug, timestamp);
        if (changeSlugResult.IsFailure)
        {
            return Result<bool>.Failure(changeSlugResult.Error);
        }

        // No-op: category didn't change
        if (changeSlugResult.Value is false)
        {
            return Result<bool>.Success(false);
        }

        var newBase = category.Path;

        var rebaseResult = RebaseCategories(oldBase, newBase, descendants, timestamp);
        return rebaseResult.IsSuccess switch
        {
            true => Result<bool>.Success(true),
            false => rebaseResult
        };
    }

    /// <summary>
    /// Archives a category and all its descendants by setting their DeletedAt timestamps.
    /// </summary>
    /// <param name="category">The category to archive.</param>
    /// <param name="descendants">The descendants of the category being archived.</param>
    /// <param name="timestamp">The timestamp of this archival operation.</param>
    /// <returns>
    /// A Result containing <c>true</c> if the subject category was archived (regardless of descendant outcomes),
    /// <c>false</c> if the subject category is already archived (no-op),
    /// or a failure with a domain error.
    /// </returns>
    public Result<bool> ArchiveCategoryAndDescendants(
        Category category,
        IReadOnlyList<Category> descendants,
        DateTimeOffset timestamp)
    {
        var archiveResult = category.Archive(timestamp);
        if (archiveResult.IsFailure)
        {
            return Result<bool>.Failure(archiveResult.Error);
        }

        // No-op: category is already archived
        if (archiveResult.Value is false)
        {
            return Result<bool>.Success(false);
        }

        foreach (var descendant in descendants)
        {
            descendant.Archive(timestamp);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Restores a category and all its descendants by clearing their DeletedAt timestamps.
    /// </summary>
    /// <param name="category">The category to restore.</param>
    /// <param name="descendants">The descendants of the category being restored.</param>
    /// <returns>
    /// A Result containing <c>true</c> if the subject category was restored (regardless of descendant outcomes),
    /// <c>false</c> if the subject category is already restored (no-op),
    /// or a failure with a domain error.
    /// </returns>
    public Result<bool> RestoreCategoryAndDescendants(
        Category category,
        IReadOnlyList<Category> descendants)
    {
        var restoreResult = category.Restore();
        if (restoreResult.IsFailure)
        {
            return Result<bool>.Failure(restoreResult.Error);
        }

        // No-op: category is already restored
        if (restoreResult.Value is false)
        {
            return Result<bool>.Success(false);
        }

        foreach (var descendant in descendants)
        {
            descendant.Restore();
        }

        return Result<bool>.Success(true);
    }
}
