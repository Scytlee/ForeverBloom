using ForeverBloom.Domain.Abstractions;
using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

public sealed class Product : Entity, ISoftDeleteable
{
    // Constants
    public const int MaxImageCount = 20;
    public const int DeletionGracePeriodInHours = 24;

    // Properties
    public ProductName Name { get; private set; } = null!;
    public SeoTitle? SeoTitle { get; private set; }
    public HtmlFragment? FullDescription { get; private set; }
    public MetaDescription? MetaDescription { get; private set; }
    public Slug CurrentSlug { get; private set; } = null!;
    public long CategoryId { get; private set; }
    public Money? Price { get; private set; } // Nullable for negotiable, made to order, or unknown pricing
    public bool IsFeatured { get; private set; }
    public PublishStatus PublishStatus { get; private set; } = PublishStatus.Draft;
    public ProductAvailabilityStatus Availability { get; private set; } = ProductAvailabilityStatus.ComingSoon;
    public ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    // Navigation properties
    public Category Category { get; private set; } = null!;

    // ISoftDeleteable
    public DateTimeOffset? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private Product() { }

    private Product(
        ProductName name,
        Slug slug,
        long categoryId,
        DateTimeOffset timestamp,
        SeoTitle? seoTitle = null,
        HtmlFragment? fullDescription = null,
        MetaDescription? metaDescription = null,
        Money? price = null,
        bool isFeatured = false,
        ProductAvailabilityStatus? availability = null,
        ICollection<ProductImage>? images = null) : base(timestamp)
    {
        availability ??= ProductAvailabilityStatus.ComingSoon;
        var imageList = images?.ToList() ?? [];

        Name = name;
        SeoTitle = seoTitle;
        FullDescription = fullDescription;
        MetaDescription = metaDescription;
        CurrentSlug = slug;
        CategoryId = categoryId;
        Price = price;
        IsFeatured = isFeatured;
        Availability = availability;
        Images = imageList;
    }

    /// <summary>
    /// Creates a new Product with business rule validation.
    /// </summary>
    public static Result<Product> Create(
        ProductName name,
        Slug slug,
        long categoryId,
        DateTimeOffset timestamp,
        SeoTitle? seoTitle = null,
        HtmlFragment? fullDescription = null,
        MetaDescription? metaDescription = null,
        Money? price = null,
        bool isFeatured = false,
        ProductAvailabilityStatus? availabilityStatus = null,
        ICollection<ProductImage>? images = null)
    {
        availabilityStatus ??= ProductAvailabilityStatus.ComingSoon;
        images ??= [];

        var errors = new List<IError>();

        if (categoryId <= 0)
        {
            errors.Add(new ProductErrors.CategoryIdInvalid(categoryId));
        }

        if (images.Count > 0)
        {
            var imageValidationResult = ValidateImageCollection(images);
            if (imageValidationResult.IsFailure)
            {
                errors.Add(imageValidationResult.Error);
            }
        }

        return Result<Product>.FromValidation(
            errors,
            () => new Product(
                name,
                slug,
                categoryId,
                timestamp,
                seoTitle,
                fullDescription,
                metaDescription,
                price,
                isFeatured,
                availabilityStatus,
                images));
    }

    /// <summary>
    /// Updates product content and metadata fields.
    /// </summary>
    /// <returns>
    /// A result containing <c>true</c> when the product was updated (callers should persist)
    /// or <c>false</c> when the request was a no-op because no fields were set.
    /// </returns>
    public Result<bool> Update(
        DateTimeOffset timestamp,
        Optional<ProductName> name = default,
        Optional<long> categoryId = default,
        Optional<SeoTitle?> seoTitle = default,
        Optional<HtmlFragment?> fullDescription = default,
        Optional<MetaDescription?> metaDescription = default,
        Optional<Money?> price = default,
        Optional<bool> isFeatured = default,
        Optional<ProductAvailabilityStatus> availability = default,
        Optional<PublishStatus> publishStatus = default)
    {
        // No-op detection: if nothing will actually change, return early
        var hasChanges = (name.IsSet && Name != name.Value) ||
                         (categoryId.IsSet && CategoryId != categoryId.Value) ||
                         (seoTitle.IsSet && SeoTitle != seoTitle.Value) ||
                         (fullDescription.IsSet && FullDescription != fullDescription.Value) ||
                         (metaDescription.IsSet && MetaDescription != metaDescription.Value) ||
                         (price.IsSet && Price != price.Value) ||
                         (isFeatured.IsSet && IsFeatured != isFeatured.Value) ||
                         (availability.IsSet && Availability != availability.Value) ||
                         (publishStatus.IsSet && PublishStatus != publishStatus.Value);

        if (!hasChanges)
        {
            return Result<bool>.Success(false);
        }

        var errors = new List<IError>();

        if (categoryId is { IsSet: true, Value: <= 0 })
        {
            errors.Add(new ProductErrors.CategoryIdInvalid(categoryId.Value));
        }

        if (publishStatus.IsSet && PublishStatus != publishStatus.Value)
        {
            if (!PublishStatus.CanTransitionTo(publishStatus.Value))
            {
                errors.Add(new ProductErrors.PublishStatusTransitionNotAllowed(PublishStatus.Name, publishStatus.Value.Name));
            }
        }

        if (errors.Count > 0)
        {
            return Result<bool>.Failure(new CompositeError(errors));
        }

        if (name.IsSet)
            Name = name.Value;

        if (categoryId.IsSet)
            CategoryId = categoryId.Value;

        if (seoTitle.IsSet)
            SeoTitle = seoTitle.Value;

        if (fullDescription.IsSet)
            FullDescription = fullDescription.Value;

        if (metaDescription.IsSet)
            MetaDescription = metaDescription.Value;

        if (price.IsSet)
            Price = price.Value;

        if (isFeatured.IsSet)
            IsFeatured = isFeatured.Value;

        if (availability.IsSet)
            Availability = availability.Value;

        if (publishStatus.IsSet)
            PublishStatus = publishStatus.Value;

        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Changes the product's slug.
    /// Returns success without persisting if the new slug matches the current one.
    /// </summary>
    /// <param name="newSlug">The new slug to assign to the product.</param>
    /// <param name="timestamp">The timestamp applied when the slug changes.</param>
    /// <returns>
    /// A result containing <c>true</c> when the slug was updated (callers should persist)
    /// or <c>false</c> when the request was a no-op because the slug was already set.
    /// </returns>
    public Result<bool> ChangeSlug(Slug newSlug, DateTimeOffset timestamp)
    {
        if (CurrentSlug.Value == newSlug.Value)
        {
            return Result<bool>.Success(false);
        }

        CurrentSlug = newSlug;
        UpdatedAt = timestamp;

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Replaces the image collection after validating domain invariants.
    /// </summary>
    public Result UpdateImages(ICollection<ProductImage> images, DateTimeOffset timestamp)
    {
        var validationResult = ValidateImageCollection(images);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Images.Clear();
        foreach (var image in images)
        {
            Images.Add(image);
        }

        UpdatedAt = timestamp;

        return Result.Success();
    }

    /// <summary>
    /// Validates image collection business invariants.
    /// </summary>
    /// <remarks>
    /// Validates that the image collection satisfies business rules:
    /// <list type="bullet">
    /// <item><description>Collection does not exceed maximum image count</description></item>
    /// <item><description>Non-empty collections have exactly one primary image</description></item>
    /// </list>
    /// </remarks>
    /// <param name="images">The image collection to validate.</param>
    /// <returns>
    /// A result indicating success if all validations pass, or failure with a composite error
    /// containing all validation errors.
    /// </returns>
    private static Result ValidateImageCollection(ICollection<ProductImage> images)
    {
        var errors = new List<IError>();

        // Validate maximum image count
        if (images.Count > MaxImageCount)
        {
            errors.Add(new ProductErrors.TooManyImages(images.Count));
        }

        // Validate primary image rules for non-empty collections
        if (images.Count > 0)
        {
            var primaryCount = images.Count(image => image.IsPrimary);
            if (primaryCount == 0)
            {
                errors.Add(new ProductErrors.NoPrimaryImage());
            }
            else if (primaryCount > 1)
            {
                var primaryIndices = images
                    .Select((img, index) => new { img, index })
                    .Where(x => x.img.IsPrimary)
                    .Select(x => x.index)
                    .ToArray();

                errors.Add(new ProductErrors.MultiplePrimaryImages(primaryIndices));
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure(new CompositeError(errors));
        }

        return Result.Success();
    }

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

public static class ProductErrors
{
    public sealed record CategoryIdInvalid([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Product.CategoryIdInvalid";
        public override string Message => $"Category ID must be greater than 0, but was {Id}";
    }

    public sealed record NoPrimaryImage : DomainError
    {
        public override string Code => "Product.NoPrimaryImage";
        public override string Message => "Product must have exactly one primary image when images are provided";
    }

    public sealed record MultiplePrimaryImages([AttemptedValue] int[] Indices) : DomainError
    {
        public override string Code => "Product.MultiplePrimaryImages";
        public override string Message => $"Only one image can be marked as primary, but {Indices.Length} images at indices [{string.Join(", ", Indices)}] are marked as primary";
    }

    public sealed record PublishStatusTransitionNotAllowed([CurrentValue] string CurrentStatus, [AttemptedValue] string AttemptedStatus) : DomainError
    {
        public override string Code => "Product.PublishStatusTransitionNotAllowed";
        public override string Message => $"Cannot transition publish status from '{CurrentStatus}' to '{AttemptedStatus}'";
    }

    public sealed record TooManyImages([AttemptedValue] int Count) : DomainError
    {
        public override string Code => "Product.Images.TooMany";
        public override string Message => $"Product can have at most {MaxImageCount} images, but {Count} were provided";
        public int MaxImageCount => Product.MaxImageCount;
    }

    /// <summary>
    /// Error indicating a product was not found via slug lookup.
    /// </summary>
    public sealed record NotFoundBySlug([AttemptedValue] string Slug) : DomainError
    {
        public override string Code => "Product.NotFoundBySlug";
        public override string Message => $"Product with slug '{Slug}' was not found";
    }

    /// <summary>
    /// Error indicating a product was not found via ID lookup.
    /// </summary>
    public sealed record NotFoundById([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Product.NotFoundById";
        public override string Message => $"Product with ID {Id} was not found";
    }

    /// <summary>
    /// Error indicating a product slug has changed and requires a redirect.
    /// </summary>
    public sealed record SlugChanged([AttemptedValue] string AttemptedSlug, [CurrentValue] string CurrentSlug) : DomainError
    {
        public override string Code => "Product.SlugChanged";
        public override string Message => $"The product slug has changed from '{AttemptedSlug}' to '{CurrentSlug}'";
    }

    /// <summary>
    /// Error indicating a slug is already in use and not available for a new product.
    /// </summary>
    public sealed record SlugNotAvailable([AttemptedValue] string Slug) : DomainError
    {
        public override string Code => "Product.SlugNotAvailable";
        public override string Message => $"The slug '{Slug}' is already in use";
    }

    /// <summary>
    /// Error indicating the specified category was not found.
    /// </summary>
    public sealed record CategoryNotFound([AttemptedValue] long CategoryId) : DomainError
    {
        public override string Code => "Product.CategoryNotFound";
        public override string Message => $"Category with ID {CategoryId} was not found";
    }

    /// <summary>
    /// Error indicating the supplied product ID is not valid.
    /// </summary>
    public sealed record ProductIdInvalid([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Product.IdInvalid";
        public override string Message => $"Product ID must be greater than 0, but was {Id}";
    }

    /// <summary>
    /// Error indicating a product image was not found.
    /// </summary>
    public sealed record ImageNotFound([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Product.ImageNotFound";
        public override string Message => $"The product image with ID {Id} was not found";
    }

    /// <summary>
    /// Error indicating a product image ID is invalid.
    /// </summary>
    public sealed record ImageIdInvalid([AttemptedValue] long Id) : DomainError
    {
        public override string Code => "Product.ImageIdInvalid";
        public override string Message => $"Image ID must be greater than 0, but was {Id}";
    }

    /// <summary>
    /// Error indicating a product image ID is duplicated.
    /// </summary>
    public sealed record DuplicateImageIds([AttemptedValue] IReadOnlyList<long> Ids) : DomainError
    {
        public override string Code => "Product.DuplicateImageIds";
        public override string Message => $"The following image IDs are duplicated: {string.Join(", ", Ids)}";
    }

    /// <summary>
    /// Error indicating a product cannot be deleted because it is not archived.
    /// </summary>
    public sealed record CannotDeleteNotArchived([AttemptedValue] long ProductId) : DomainError
    {
        public override string Code => "Product.CannotDeleteNotArchived";
        public override string Message => $"Product with ID {ProductId} must be archived before it can be deleted";
    }

    /// <summary>
    /// Error indicating a product cannot be deleted because insufficient time has passed since archival.
    /// </summary>
    public sealed record CannotDeleteTooSoon([AttemptedValue] long ProductId, DateTimeOffset ArchivedAt, DateTimeOffset EligibleAt) : DomainError
    {
        public override string Code => "Product.CannotDeleteTooSoon";
        public override string Message => $"Product with ID {ProductId} was archived at {ArchivedAt:u} and can be deleted after {EligibleAt:u}";
    }
}
