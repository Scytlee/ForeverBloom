using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories;

/// <summary>
/// Category feature-specific error definitions.
/// </summary>
public static class CategoryErrors
{
    /// <summary>
    /// Error indicating an invalid category ID.
    /// </summary>
    public sealed record CategoryIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Category.CategoryIdInvalid";
        public string Message => "Category ID must be greater than 0";
    }

    /// <summary>
    /// Error indicating a slug is already in use and cannot be assigned to a new category.
    /// </summary>
    public sealed record SlugNotAvailable(string Slug) : IError
    {
        public string Code => "Category.SlugNotAvailable";
        public string Message => $"The slug '{Slug}' is already in use.";
    }

    /// <summary>
    /// Error raised when the specified parent category does not exist.
    /// </summary>
    public sealed record ParentNotFound(long ParentCategoryId) : IError
    {
        public string Code => "Category.ParentNotFound";
        public string Message => $"Parent category with ID {ParentCategoryId} was not found.";
    }

    /// <summary>
    /// Error raised when a sibling category already uses the provided name.
    /// </summary>
    public sealed record NameNotUniqueWithinParent(string Name, long? ParentCategoryId) : IError
    {
        public string Code => "Category.NameNotUniqueWithinParent";
        public string Message => ParentCategoryId is null
            ? $"A root category named '{Name}' already exists."
            : $"A category named '{Name}' already exists under parent ID {ParentCategoryId}.";
    }

    /// <summary>
    /// Error raised when attempting to perform an operation on a category with too many descendants.
    /// </summary>
    public sealed record TooManyDescendants(long CategoryId) : IError
    {
        public string Code => "Category.TooManyDescendants";
        public string Message => $"Cannot perform the operation on category {CategoryId} because it has more than {MaximumAllowedDescendants} descendants.";
        public int MaximumAllowedDescendants => Category.DescendantLimitOnUpdate;
    }

    /// <summary>
    /// Error raised when attempting to restore a category that has archived ancestors.
    /// </summary>
    public sealed record HasArchivedAncestors(long CategoryId) : IError
    {
        public string Code => "Category.HasArchivedAncestors";
        public string Message => $"Cannot restore category {CategoryId} because it has one or more archived ancestors.";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it is not archived.
    /// </summary>
    public sealed record CannotDeleteNotArchived(long CategoryId) : IError
    {
        public string Code => "Category.CannotDeleteNotArchived";
        public string Message => $"Category with ID {CategoryId} must be archived before it can be deleted.";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because insufficient time has passed since archival.
    /// </summary>
    public sealed record CannotDeleteTooSoon(
        long CategoryId,
        DateTimeOffset ArchivedAt,
        DateTimeOffset EligibleAt) : IError
    {
        public string Code => "Category.CannotDeleteTooSoon";
        public string Message => $"Category with ID {CategoryId} was archived at {ArchivedAt:u} and can be deleted after {EligibleAt:u}.";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it has children categories.
    /// </summary>
    public sealed record CannotDeleteHasChildren(long CategoryId) : IError
    {
        public string Code => "Category.CannotDeleteHasChildren";
        public string Message => $"Cannot delete category with ID {CategoryId} because it has children.";
    }

    /// <summary>
    /// Error indicating a category cannot be deleted because it has products referencing it.
    /// </summary>
    public sealed record CannotDeleteHasProducts(long CategoryId) : IError
    {
        public string Code => "Category.CannotDeleteHasProducts";
        public string Message => $"Cannot delete category with ID {CategoryId} because it has products referencing it.";
    }

    /// <summary>
    /// Base error indicating a category was not found.
    /// </summary>
    public abstract record NotFound : IError
    {
        public string Code => "Category.NotFound";
        public string Message => "Category was not found.";
    }

    /// <summary>
    /// Error indicating a category was not found via slug lookup.
    /// </summary>
    public sealed record NotFoundBySlug(string AttemptedSlug) : NotFound;

    /// <summary>
    /// Error indicating a category was not found via ID lookup.
    /// </summary>
    public sealed record NotFoundById(long AttemptedId) : NotFound;

    /// <summary>
    /// Error indicating the category slug has changed to a new value.
    /// </summary>
    public sealed record SlugChanged(string AttemptedSlug, string CurrentSlug) : IError
    {
        public string Code => "Category.SlugChanged";
        public string Message => "The category slug has changed.";
    }
}
