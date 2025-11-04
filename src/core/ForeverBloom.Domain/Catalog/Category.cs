using ForeverBloom.Domain.Abstractions;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed class Category : Entity, ISoftDeleteable
{
    // Properties
    public SeoTitle Name { get; private set; } = null!;
    public MetaDescription? Description { get; private set; }
    public Slug CurrentSlug { get; private set; } = null!;
    public Image? Image { get; private set; }
    public HierarchicalPath Path { get; private set; } = null!;
    public long? ParentCategoryId { get; private set; }
    public int DisplayOrder { get; private set; }
    public PublishStatus PublishStatus { get; private set; } = PublishStatus.Draft;

    // Navigation properties
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> ChildCategories { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    // ISoftDeleteable
    public DateTimeOffset? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private Category() { }

    private Category(
        SeoTitle name,
        MetaDescription? description,
        Slug slug,
        Image? image,
        HierarchicalPath path,
        long? parentCategoryId,
        int displayOrder,
        DateTimeOffset timestamp) : base(timestamp)
    {
        Name = name;
        Description = description;
        CurrentSlug = slug;
        Image = image;
        Path = path;
        ParentCategoryId = parentCategoryId;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Creates a new Category with domain validation.
    /// </summary>
    public static Result<Category> Create(
        SeoTitle name,
        MetaDescription? description,
        Slug slug,
        Image? image,
        HierarchicalPath path,
        long? parentCategoryId,
        int displayOrder,
        DateTimeOffset timestamp)
    {
        var errors = new List<IError>();

        if (parentCategoryId is <= 0)
        {
            errors.Add(new CategoryErrors.ParentCategoryIdInvalid(parentCategoryId.Value));
        }

        return Result<Category>.FromValidation(
            errors,
            () => new Category(
                name,
                description,
                slug,
                image,
                path,
                parentCategoryId,
                displayOrder,
                timestamp));
    }

    /// <summary>
    /// Updates category content and metadata fields.
    /// </summary>
    /// <returns>
    /// A result containing <c>true</c> when the category was updated (callers should persist)
    /// or <c>false</c> when the request was a no-op because no fields were set.
    /// </returns>
    public Result<bool> Update(
        Optional<SeoTitle> name,
        Optional<MetaDescription?> description,
        Optional<Image?> image,
        Optional<int> displayOrder,
        Optional<PublishStatus> publishStatus,
        DateTimeOffset timestamp)
    {
        // No-op detection: if nothing will actually change, return early
        var hasChanges = (name.IsSet && Name != name.Value) ||
                         (description.IsSet && Description != description.Value) ||
                         (image.IsSet && Image != image.Value) ||
                         (displayOrder.IsSet && DisplayOrder != displayOrder.Value) ||
                         (publishStatus.IsSet && PublishStatus != publishStatus.Value);

        if (!hasChanges)
        {
            return Result<bool>.Success(false);
        }

        var errors = new List<IError>();

        if (publishStatus.IsSet && PublishStatus != publishStatus.Value)
        {
            if (!PublishStatus.CanTransitionTo(publishStatus.Value))
            {
                errors.Add(new CategoryErrors.PublishStatusTransitionNotAllowed(PublishStatus, publishStatus.Value));
            }
        }

        if (errors.Count > 0)
        {
            return Result<bool>.Failure(new CompositeError(errors));
        }

        if (name.IsSet)
            Name = name.Value;

        if (description.IsSet)
            Description = description.Value;

        if (image.IsSet)
            Image = image.Value;

        if (displayOrder.IsSet)
            DisplayOrder = displayOrder.Value;

        if (publishStatus.IsSet)
            PublishStatus = publishStatus.Value;

        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Changes the category's slug and updates its path accordingly.
    /// The new path is computed internally by the HierarchicalPath value object.
    /// </summary>
    /// <param name="newSlug">The new slug to apply.</param>
    /// <param name="timestamp">The timestamp of this change.</param>
    /// <returns>
    /// A result containing <c>true</c> when the slug was changed (callers should persist and update descendants)
    /// or <c>false</c> when the slug is already set to this value (no-op).
    /// </returns>
    public Result<bool> ChangeSlug(Slug newSlug, DateTimeOffset timestamp)
    {
        // No-op detection
        if (CurrentSlug == newSlug)
        {
            return Result<bool>.Success(false);
        }

        // Let HierarchicalPath compute the new path with the specified slug
        var newPathResult = Path.WithSlug(newSlug);

        if (newPathResult.IsFailure)
        {
            return Result<bool>.Failure(newPathResult.Error);
        }

        CurrentSlug = newSlug;
        Path = newPathResult.Value;
        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Reparents the category to a new parent and updates its path accordingly.
    /// The subject's new path is computed from the parent's path and the current slug.
    /// </summary>
    /// <param name="newParentId">The new parent category ID, or null for root categories.</param>
    /// <param name="newParentPath">The hierarchical path of the new parent (null for root).</param>
    /// <param name="timestamp">The timestamp of this change.</param>
    /// <returns>
    /// A result containing <c>true</c> when the parent was changed (callers should persist and update descendants)
    /// or <c>false</c> when the parent is already set to this value (no-op).
    /// </returns>
    public Result<bool> Reparent(long? newParentId, HierarchicalPath? newParentPath, DateTimeOffset timestamp)
    {
        var errors = new List<IError>();

        // Validate parent ID if provided
        if (newParentId is <= 0)
        {
            errors.Add(new CategoryErrors.ParentCategoryIdInvalid(newParentId.Value));
        }

        // Cannot be own parent
        if (newParentId == Id)
        {
            errors.Add(new CategoryErrors.CannotBeOwnParent(Id));
        }

        if (errors.Count > 0)
        {
            return Result<bool>.Failure(new CompositeError(errors));
        }

        // Compute the new path from parent path + current slug
        Result<HierarchicalPath> newPathResult;
        if (newParentPath == null)
        {
            // Root category - path is just the slug
            newPathResult = HierarchicalPath.FromString(CurrentSlug.Value);
        }
        else
        {
            // Child category - combine parent path with slug
            newPathResult = HierarchicalPath.FromParent(newParentPath, CurrentSlug);
        }

        if (newPathResult.IsFailure)
        {
            return Result<bool>.Failure(newPathResult.Error);
        }

        var newPath = newPathResult.Value;

        // No-op detection
        if (ParentCategoryId == newParentId && Path.Value == newPath.Value)
        {
            return Result<bool>.Success(false);
        }

        ParentCategoryId = newParentId;
        Path = newPath;
        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Rebases the category's path by swapping the old base prefix with a new base prefix.
    /// This is typically used for descendants when their ancestor's path changes.
    /// </summary>
    /// <param name="oldBase">The old base path prefix to replace.</param>
    /// <param name="newBase">The new base path prefix.</param>
    /// <param name="timestamp">The timestamp of this change.</param>
    /// <returns>
    /// A result containing <c>true</c> when the path was changed (callers should persist)
    /// or <c>false</c> when the rebase results in no change (no-op).
    /// </returns>
    public Result<bool> RebasePath(HierarchicalPath oldBase, HierarchicalPath newBase, DateTimeOffset timestamp)
    {
        // Use the HierarchicalPath.Rebase method to perform the prefix swap
        var rebaseResult = HierarchicalPath.Rebase(Path, oldBase, newBase);

        if (rebaseResult.IsFailure)
        {
            return Result<bool>.Failure(rebaseResult.Error);
        }

        var newPath = rebaseResult.Value;

        // No-op detection - if path didn't change, return false
        if (Path.Value == newPath.Value)
        {
            return Result<bool>.Success(false);
        }

        Path = newPath;
        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }
}

public static class CategoryErrors
{
    public sealed record ParentCategoryIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Category.ParentCategoryIdInvalid";
        public string Message => "Parent category ID must be greater than 0";
    }

    public sealed record PublishStatusTransitionNotAllowed(PublishStatus CurrentStatus, PublishStatus AttemptedStatus) : IError
    {
        public string Code => "Category.PublishStatusTransitionNotAllowed";
        public string Message => $"Cannot transition publish status with code '{CurrentStatus.Code}' to '{AttemptedStatus.Code}'.";
    }

    public sealed record CannotBeOwnParent(long CategoryId) : IError
    {
        public string Code => "Category.CannotBeOwnParent";
        public string Message => $"Category with ID {CategoryId} cannot be its own parent";
    }

    public sealed record CircularDependency(long CategoryId, long AttemptedParentId) : IError
    {
        public string Code => "Category.CircularDependency";
        public string Message => $"Cannot set category {AttemptedParentId} as parent of category {CategoryId} because it would create a circular dependency";
    }
}
