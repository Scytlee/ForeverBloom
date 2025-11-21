using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand : ICommand<CreateProductResult>, IWithTransactionOverrides
{
    public ProductName Name { get; private init; } = null!;
    public SeoTitle? SeoTitle { get; private init; }
    public HtmlFragment? FullDescription { get; private init; }
    public MetaDescription? MetaDescription { get; private init; }
    public Slug Slug { get; private init; } = null!;
    public long CategoryId { get; private init; }
    public Money? Price { get; private init; }
    public bool IsFeatured { get; private init; }
    public ProductAvailabilityStatus AvailabilityStatus { get; private init; } = null!;
    public ProductImage[] Images { get; private init; } = [];

    public TransactionSettings TransactionSettings { get; } = new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };

    private CreateProductCommand() { }

    public static Result<CreateProductCommand> Create(
        string name,
        string slug,
        long categoryId,
        bool isFeatured = false,
        string availabilityStatus = "coming_soon",
        string? seoTitle = null,
        string? fullDescription = null,
        string? metaDescription = null,
        decimal? price = null,
        CreateProductCommandImage[]? images = null)
    {
        var errors = new List<IError>();

        // ProductName (required)
        var nameResult = ProductName.Create(name);
        if (nameResult.IsFailure)
        {
            errors.Add(nameResult.Error);
        }

        // Slug (required)
        var slugResult = Slug.Create(slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        // CategoryId (required, must be positive)
        if (categoryId <= 0)
        {
            errors.Add(new ProductErrors.CategoryIdInvalid(categoryId));
        }

        // AvailabilityStatus (optional, defaults to "coming_soon")
        var availabilityStatusResult = ProductAvailabilityStatus.FromName(availabilityStatus);
        if (availabilityStatusResult.IsFailure)
        {
            errors.Add(availabilityStatusResult.Error);
        }

        // SeoTitle (optional)
        Result<SeoTitle>? seoTitleResult = null;
        if (!string.IsNullOrWhiteSpace(seoTitle))
        {
            seoTitleResult = SeoTitle.Create(seoTitle);
            if (seoTitleResult.IsFailure)
            {
                errors.Add(seoTitleResult.Error);
            }
        }

        // MetaDescription (optional)
        Result<MetaDescription>? metaDescriptionResult = null;
        if (!string.IsNullOrWhiteSpace(metaDescription))
        {
            metaDescriptionResult = MetaDescription.Create(metaDescription);
            if (metaDescriptionResult.IsFailure)
            {
                errors.Add(metaDescriptionResult.Error);
            }
        }

        // FullDescription (optional)
        Result<HtmlFragment>? fullDescriptionResult = null;
        if (!string.IsNullOrWhiteSpace(fullDescription))
        {
            fullDescriptionResult = HtmlFragment.Create(fullDescription);
            if (fullDescriptionResult.IsFailure)
            {
                errors.Add(fullDescriptionResult.Error);
            }
        }

        // Price (optional)
        Result<Money>? priceResult = null;
        if (price.HasValue)
        {
            priceResult = Money.Create(price.Value);
            if (priceResult.IsFailure)
            {
                errors.Add(priceResult.Error);
            }
        }

        // Images (optional collection)
        ProductImage[] productImages = [];
        if (images is { Length: > 0 })
        {
            var imageValueObjects = new List<ProductImage>(images.Length);

            foreach (var imageInput in images)
            {
                var imageResult = Image.Create(imageInput.Source, imageInput.AltText);
                if (imageResult.IsFailure)
                {
                    errors.Add(imageResult.Error);
                    continue;
                }

                imageValueObjects.Add(ProductImage.Create(
                    imageResult.Value,
                    imageInput.IsPrimary,
                    imageInput.DisplayOrder));
            }

            // Only assign if we successfully created some images
            if (imageValueObjects.Count > 0)
            {
                productImages = imageValueObjects.ToArray();
            }
        }

        return Result<CreateProductCommand>.FromValidation(
            errors,
            () => new CreateProductCommand
            {
                Name = nameResult.Value!,
                SeoTitle = seoTitleResult?.Value,
                FullDescription = fullDescriptionResult?.Value,
                MetaDescription = metaDescriptionResult?.Value,
                Slug = slugResult.Value!,
                CategoryId = categoryId,
                Price = priceResult?.Value,
                IsFeatured = isFeatured,
                AvailabilityStatus = availabilityStatusResult.Value!,
                Images = productImages
            });
    }
}

/// <summary>
/// Represents a single image supplied when creating a product.
/// </summary>
public sealed record CreateProductCommandImage(
    string Source,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder);
