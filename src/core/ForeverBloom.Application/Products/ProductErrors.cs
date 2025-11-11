using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products;

/// <summary>
/// Product feature error definitions.
/// </summary>
public static class ProductErrors
{
    /// <summary>
    /// Base error indicating a product was not found.
    /// </summary>
    public abstract record NotFound : IError
    {
        public string Code => "Product.NotFound";
        public string Message => "Product was not found.";
    }

    /// <summary>
    /// Error indicating a product was not found via slug lookup.
    /// </summary>
    public sealed record NotFoundBySlug(string AttemptedSlug) : NotFound;

    /// <summary>
    /// Error indicating a product was not found via ID lookup.
    /// </summary>
    public sealed record NotFoundById(long AttemptedId) : NotFound;

    /// <summary>
    /// Error indicating a product slug has changed and requires a redirect.
    /// </summary>
    public sealed record SlugChanged(string AttemptedSlug, string CurrentSlug) : IError
    {
        public string Code => "Product.SlugChanged";
        public string Message => "The product slug has changed.";
    }

    /// <summary>
    /// Error indicating a slug is already in use and not available for a new product.
    /// </summary>
    public sealed record SlugNotAvailable(string Slug) : IError
    {
        public string Code => "Product.SlugNotAvailable";
        public string Message => $"The slug '{Slug}' is already in use.";
    }

    /// <summary>
    /// Error indicating the specified category was not found.
    /// </summary>
    public sealed record CategoryNotFound(long CategoryId) : IError
    {
        public string Code => "Product.CategoryNotFound";
        public string Message => $"Category with ID {CategoryId} was not found.";
    }

    /// <summary>
    /// Error indicating the supplied product ID is not valid.
    /// </summary>
    public sealed record ProductIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Product.IdInvalid";
        public string Message => $"Product ID must be greater than 0, but was {AttemptedId}.";
    }

    /// <summary>
    /// Error indicating a product image was not found.
    /// </summary>
    public sealed record ImageNotFound(long AttemptedId) : IError
    {
        public string Code => "Product.ImageNotFound";
        public string Message => $"The product image with ID {AttemptedId} was not found.";
    }

    /// <summary>
    /// Error indicating a product image ID is invalid.
    /// </summary>
    public sealed record ImageIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Product.ImageIdInvalid";
        public string Message => $"Image ID must be greater than 0, but was {AttemptedId}.";
    }

    /// <summary>
    /// Error indicating a product image ID is duplicated.
    /// </summary>
    /// <param name="DuplicateIds"></param>
    public sealed record DuplicateImageIds(IReadOnlyList<long> DuplicateIds) : IError
    {
        public string Code => "Product.DuplicateImageIds";
        public string Message => $"The following image IDs are duplicated: {string.Join(", ", DuplicateIds)}.";
    }

    /// <summary>
    /// Error indicating a product cannot be deleted because it is not archived.
    /// </summary>
    public sealed record CannotDeleteNotArchived(long ProductId) : IError
    {
        public string Code => "Product.CannotDeleteNotArchived";
        public string Message => $"Product with ID {ProductId} must be archived before it can be deleted.";
    }

    /// <summary>
    /// Error indicating a product cannot be deleted because insufficient time has passed since archival.
    /// </summary>
    public sealed record CannotDeleteTooSoon(long ProductId, DateTimeOffset ArchivedAt, DateTimeOffset EligibleAt) : IError
    {
        public string Code => "Product.CannotDeleteTooSoon";
        public string Message => $"Product with ID {ProductId} was archived at {ArchivedAt:u} and can be deleted after {EligibleAt:u}.";
    }
}
