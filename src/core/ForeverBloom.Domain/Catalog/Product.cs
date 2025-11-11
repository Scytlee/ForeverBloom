using ForeverBloom.Domain.Abstractions;
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
        SeoTitle? seoTitle,
        HtmlFragment? fullDescription,
        MetaDescription? metaDescription,
        Slug slug,
        long categoryId,
        Money? price,
        bool isFeatured,
        ProductAvailabilityStatus availability,
        List<ProductImage> images,
        DateTimeOffset timestamp) : base(timestamp)
    {
        Name = name;
        SeoTitle = seoTitle;
        FullDescription = fullDescription;
        MetaDescription = metaDescription;
        CurrentSlug = slug;
        CategoryId = categoryId;
        Price = price;
        IsFeatured = isFeatured;
        Availability = availability;
        Images = images;
    }

    /// <summary>
    /// Creates a new Product with business rule validation.
    /// </summary>
    public static Result<Product> Create(
        ProductName name,
        SeoTitle? seoTitle,
        HtmlFragment? fullDescription,
        MetaDescription? metaDescription,
        Slug slug,
        long categoryId,
        Money? price,
        bool isFeatured,
        ProductAvailabilityStatus availabilityStatus,
        DateTimeOffset timestamp,
        ICollection<ProductImage>? images = null)
    {
        var errors = new List<IError>();

        if (categoryId <= 0)
        {
            errors.Add(new ProductErrors.CategoryIdInvalid(categoryId));
        }

        if (images is not null && images.Count > 0)
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
                seoTitle,
                fullDescription,
                metaDescription,
                slug,
                categoryId,
                price,
                isFeatured,
                availabilityStatus,
                images?.ToList() ?? [],
                timestamp));
    }

    /// <summary>
    /// Updates product content and metadata fields.
    /// </summary>
    /// <returns>
    /// A result containing <c>true</c> when the product was updated (callers should persist)
    /// or <c>false</c> when the request was a no-op because no fields were set.
    /// </returns>
    public Result<bool> Update(
        Optional<ProductName> name,
        Optional<SeoTitle?> seoTitle,
        Optional<HtmlFragment?> fullDescription,
        Optional<MetaDescription?> metaDescription,
        Optional<long> categoryId,
        Optional<Money?> price,
        Optional<bool> isFeatured,
        Optional<ProductAvailabilityStatus> availability,
        Optional<PublishStatus> publishStatus,
        DateTimeOffset timestamp)
    {
        // No-op detection: if nothing will actually change, return early
        var hasChanges = (name.IsSet && Name != name.Value) ||
                         (seoTitle.IsSet && SeoTitle != seoTitle.Value) ||
                         (fullDescription.IsSet && FullDescription != fullDescription.Value) ||
                         (metaDescription.IsSet && MetaDescription != metaDescription.Value) ||
                         (categoryId.IsSet && CategoryId != categoryId.Value) ||
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
                errors.Add(new ProductErrors.PublishStatusTransitionNotAllowed(PublishStatus, publishStatus.Value));
            }
        }

        if (errors.Count > 0)
        {
            return Result<bool>.Failure(new CompositeError(errors));
        }

        if (name.IsSet)
            Name = name.Value;

        if (seoTitle.IsSet)
            SeoTitle = seoTitle.Value;

        if (fullDescription.IsSet)
            FullDescription = fullDescription.Value;

        if (metaDescription.IsSet)
            MetaDescription = metaDescription.Value;

        if (categoryId.IsSet)
            CategoryId = categoryId.Value;

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
        var imageList = images.ToList();
        var errors = new List<IError>();

        // Validate maximum image count
        if (imageList.Count > MaxImageCount)
        {
            errors.Add(new ProductErrors.TooManyImages(imageList.Count));
        }

        // Validate primary image rules for non-empty collections
        if (imageList.Count > 0)
        {
            var primaryCount = imageList.Count(image => image.IsPrimary);
            if (primaryCount == 0)
            {
                errors.Add(new ProductErrors.NoPrimaryImage());
            }
            else if (primaryCount > 1)
            {
                var primaryIndices = imageList
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
    public sealed record CategoryIdInvalid(long AttemptedId) : IError
    {
        public string Code => "Product.CategoryIdInvalid";
        public string Message => "Category ID must be greater than 0";
    }

    public sealed record NoPrimaryImage : IError
    {
        public string Code => "Product.NoPrimaryImage";
        public string Message => "Product must have exactly one primary image when images are provided";
    }

    public sealed record MultiplePrimaryImages(int[] PrimaryIndices) : IError
    {
        public string Code => "Product.MultiplePrimaryImages";
        public string Message => $"Only one image can be marked as primary, but {PrimaryIndices.Length} images at indices [{string.Join(", ", PrimaryIndices)}] are marked as primary";
    }

    public sealed record PublishStatusTransitionNotAllowed(PublishStatus CurrentStatus, PublishStatus AttemptedStatus) : IError
    {
        public string Code => "Product.PublishStatusTransitionNotAllowed";
        public string Message => $"Cannot transition publish status with code '{CurrentStatus.Code}' to '{AttemptedStatus.Code}'.";
    }

    public sealed record TooManyImages(int AttemptedCount) : IError
    {
        public string Code => "Product.Images.TooMany";
        public string Message => $"Product can have at most {MaxImageCount} images, but {AttemptedCount} were provided.";
        public int MaxImageCount => Product.MaxImageCount;
    }
}
