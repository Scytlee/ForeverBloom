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
    /// Error raised when attempting to reparent a category with too many descendants.
    /// </summary>
    public sealed record TooManyDescendantsToMove(long CategoryId, int MaxAllowed) : IError
    {
        public string Code => "Category.TooManyDescendantsToMove";
        public string Message => $"Cannot reparent category {CategoryId} because it has more than {MaxAllowed} descendants.";
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
}
