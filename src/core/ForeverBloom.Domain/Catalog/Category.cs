using ForeverBloom.Domain.Abstractions;
using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed class Category : Entity, ISoftDeleteable
{
    // Constants
    public const int DescendantLimitOnUpdate = 100;
    public const int DeletionGracePeriodInHours = 24;

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
        Slug slug,
        HierarchicalPath path,
        DateTimeOffset timestamp,
        MetaDescription? description = null,
        Image? image = null,
        long? parentCategoryId = null,
        int displayOrder = 0) : base(timestamp)
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
        Slug slug,
        HierarchicalPath path,
        DateTimeOffset timestamp,
        MetaDescription? description = null,
        Image? image = null,
        long? parentCategoryId = null,
        int displayOrder = 0)
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
                slug,
                path,
                timestamp,
                description,
                image,
                parentCategoryId,
                displayOrder));
    }

    /// <summary>
    /// Updates category content and metadata fields.
    /// </summary>
    /// <returns>
    /// A result containing <c>true</c> when the category was updated (callers should persist)
    /// or <c>false</c> when the request was a no-op because no fields were set.
    /// </returns>
    public Result<bool> Update(
        DateTimeOffset timestamp,
        Optional<SeoTitle> name = default,
        Optional<MetaDescription?> description = default,
        Optional<Image?> image = default,
        Optional<int> displayOrder = default,
        Optional<PublishStatus> publishStatus = default)
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
                errors.Add(new CategoryErrors.PublishStatusTransitionNotAllowed(PublishStatus.Name, publishStatus.Value.Name));
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

        CurrentSlug = newSlug;
        Path = Path.WithSlug(newSlug);
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

    /// <summary>
    /// Archives the category by setting the DeletedAt timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp of this archival operation.</param>
    /// <returns>
    /// A result containing <c>true</c> when the category was archived (callers should persist)
    /// or <c>false</c> when the category is already archived (no-op).
    /// </returns>
    public Result<bool> Archive(DateTimeOffset timestamp)
    {
        // No-op: already archived
        if (DeletedAt is not null)
        {
            return Result<bool>.Success(false);
        }

        DeletedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Restores the category by clearing the DeletedAt timestamp.
    /// </summary>
    /// <returns>
    /// A result containing <c>true</c> when the category was restored (callers should persist)
    /// or <c>false</c> when the category is already restored (no-op).
    /// </returns>
    public Result<bool> Restore()
    {
        // No-op: already restored
        if (DeletedAt is null)
        {
            return Result<bool>.Success(false);
        }

        DeletedAt = null;

        return Result<bool>.Success(true);
    }
}

public static class CategoryErrors
{
    public sealed record ParentCategoryIdInvalid([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Category.ParentCategoryIdInvalid";
        public override string Message => $"Parent category ID must be greater than 0, but was {Id}";
    }

    public sealed record PublishStatusTransitionNotAllowed([CurrentValue] string CurrentStatus, [AttemptedValue] string AttemptedStatus) : DomainError
    {
        public override string Code => "Category.PublishStatusTransitionNotAllowed";
        public override string Message => $"Cannot transition publish status from '{CurrentStatus}' to '{AttemptedStatus}'";
    }

    public sealed record CannotBeOwnParent([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.CannotBeOwnParent";
        public override string Message => $"Category with ID {CategoryId} cannot be its own parent";
    }

    public sealed record CircularDependency([AttemptedValue] long ParentId, long CategoryId) : DomainError
    {
        public override string Code => "Category.CircularDependency";
        public override string Message => $"Cannot set category {ParentId} as parent of category {CategoryId} because it would create a circular dependency";
    }

    /// <summary>
    /// Error indicating an invalid category ID.
    /// </summary>
    public sealed record CategoryIdInvalid([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Category.CategoryIdInvalid";
        public override string Message => $"Category ID must be greater than 0, but was {Id}";
    }

    /// <summary>
    /// Error indicating a slug is already in use and cannot be assigned to a new category.
    /// </summary>
    public sealed record SlugNotAvailable([AttemptedValue] string Slug) : DomainError
    {
        public override string Code => "Category.SlugNotAvailable";
        public override string Message => $"The slug '{Slug}' is already in use";
    }

    /// <summary>
    /// Error raised when the specified parent category does not exist.
    /// </summary>
    public sealed record ParentNotFound([AttemptedValue] long ParentCategoryId) : DomainError
    {
        public override string Code => "Category.ParentNotFound";
        public override string Message => $"Parent category with ID {ParentCategoryId} was not found";
    }

    /// <summary>
    /// Error raised when a sibling category already uses the provided name.
    /// </summary>
    public sealed record NameNotUniqueWithinParent([AttemptedValue] string Name, long? ParentCategoryId) : DomainError
    {
        public override string Code => "Category.NameNotUniqueWithinParent";
        public override string Message => ParentCategoryId is null
            ? $"A root category named '{Name}' already exists"
            : $"A category named '{Name}' already exists under parent ID {ParentCategoryId}";
    }

    /// <summary>
    /// Error raised when attempting to perform an operation on a category with too many descendants.
    /// </summary>
    public sealed record TooManyDescendants([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.TooManyDescendants";
        public override string Message => $"Cannot perform the operation on category {CategoryId} because it has more than {MaximumAllowedDescendants} descendants";
        public static int MaximumAllowedDescendants => Category.DescendantLimitOnUpdate;
    }

    /// <summary>
    /// Error raised when attempting to restore a category that has archived ancestors.
    /// </summary>
    public sealed record HasArchivedAncestors([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.HasArchivedAncestors";
        public override string Message => $"Cannot restore category {CategoryId} because it has one or more archived ancestors";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it is not archived.
    /// </summary>
    public sealed record CannotDeleteNotArchived([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.CannotDeleteNotArchived";
        public override string Message => $"Category with ID {CategoryId} must be archived before it can be deleted";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because insufficient time has passed since archival.
    /// </summary>
    public sealed record CannotDeleteTooSoon(
        [AttemptedValue] long CategoryId,
        DateTimeOffset ArchivedAt,
        DateTimeOffset EligibleAt) : DomainError
    {
        public override string Code => "Category.CannotDeleteTooSoon";
        public override string Message => $"Category with ID {CategoryId} was archived at {ArchivedAt:u} and can be deleted after {EligibleAt:u}";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it has children categories.
    /// </summary>
    public sealed record CannotDeleteHasChildren([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.CannotDeleteHasChildren";
        public override string Message => $"Cannot delete category with ID {CategoryId} because it has children";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it has products referencing it.
    /// </summary>
    public sealed record CannotDeleteHasProducts([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Category.CannotDeleteHasProducts";
        public override string Message => $"Cannot delete category with ID {CategoryId} because it has products referencing it";
    }

    /// <summary>
    /// Error indicating a category was not found via slug lookup.
    /// </summary>
    public sealed record NotFoundBySlug([AttemptedValue] string Slug) : DomainError
    {
        public override string Code => "Category.NotFoundBySlug";
        public override string Message => $"Category with slug '{Slug}' was not found";
    }

    /// <summary>
    /// Error indicating a category was not found via ID lookup.
    /// </summary>
    public sealed record NotFoundById([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Category.NotFoundById";
        public override string Message => $"Category with ID {Id} was not found";
    }

    /// <summary>
    /// Error indicating the category slug has changed to a new value.
    /// </summary>
    public sealed record SlugChanged([AttemptedValue] string AttemptedSlug, [CurrentValue] string CurrentSlug) : DomainError
    {
        public override string Code => "Category.SlugChanged";
        public override string Message => $"The category slug has changed from '{AttemptedSlug}' to '{CurrentSlug}'";
    }
}
