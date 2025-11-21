using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public sealed record UpdateProductCommand : ICommand<UpdateProductResult>
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }
    public Optional<ProductName> Name { get; private init; }
    public Optional<SeoTitle?> SeoTitle { get; private init; }
    public Optional<HtmlFragment?> FullDescription { get; private init; }
    public Optional<MetaDescription?> MetaDescription { get; private init; }
    public Optional<long> CategoryId { get; private init; }
    public Optional<Money?> Price { get; private init; }
    public Optional<bool> IsFeatured { get; private init; }
    public Optional<ProductAvailabilityStatus> Availability { get; private init; }
    public Optional<PublishStatus> PublishStatus { get; private init; }

    private UpdateProductCommand() { }

    public static Result<UpdateProductCommand> Create(
        long productId,
        uint rowVersion,
        Optional<string> name = default,
        Optional<string?> seoTitle = default,
        Optional<string?> fullDescription = default,
        Optional<string?> metaDescription = default,
        Optional<long> categoryId = default,
        Optional<decimal?> price = default,
        Optional<bool> isFeatured = default,
        Optional<string> availability = default,
        Optional<string> publishStatus = default)
    {
        var errors = new List<IError>();

        // ProductId (required)
        if (productId <= 0)
        {
            errors.Add(new ProductErrors.ProductIdInvalid(productId));
        }

        // RowVersion (required)
        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        // Name (optional)
        Optional<ProductName> nameValue = default;
        if (name.IsSet)
        {
            var nameResult = ProductName.Create(name.Value);
            if (nameResult.IsFailure)
            {
                errors.Add(nameResult.Error);
            }
            else
            {
                nameValue = Optional<ProductName>.FromValue(nameResult.Value);
            }
        }

        // SeoTitle (optional, nullable)
        Optional<SeoTitle?> seoTitleValue = default;
        if (seoTitle.IsSet)
        {
            if (seoTitle.Value is null)
            {
                seoTitleValue = Optional<SeoTitle?>.FromValue(null);
            }
            else
            {
                var seoTitleResult = Domain.Shared.SeoTitle.Create(seoTitle.Value);
                if (seoTitleResult.IsFailure)
                {
                    errors.Add(seoTitleResult.Error);
                }
                else
                {
                    seoTitleValue = Optional<SeoTitle?>.FromValue(seoTitleResult.Value);
                }
            }
        }

        // FullDescription (optional, nullable)
        Optional<HtmlFragment?> fullDescriptionValue = default;
        if (fullDescription.IsSet)
        {
            if (fullDescription.Value is null)
            {
                fullDescriptionValue = Optional<HtmlFragment?>.FromValue(null);
            }
            else
            {
                var fullDescriptionResult = HtmlFragment.Create(fullDescription.Value);
                if (fullDescriptionResult.IsFailure)
                {
                    errors.Add(fullDescriptionResult.Error);
                }
                else
                {
                    fullDescriptionValue = Optional<HtmlFragment?>.FromValue(fullDescriptionResult.Value);
                }
            }
        }

        // MetaDescription (optional, nullable)
        Optional<MetaDescription?> metaDescriptionValue = default;
        if (metaDescription.IsSet)
        {
            if (metaDescription.Value is null)
            {
                metaDescriptionValue = Optional<MetaDescription?>.FromValue(null);
            }
            else
            {
                var metaDescriptionResult = Domain.Shared.MetaDescription.Create(metaDescription.Value);
                if (metaDescriptionResult.IsFailure)
                {
                    errors.Add(metaDescriptionResult.Error);
                }
                else
                {
                    metaDescriptionValue = Optional<MetaDescription?>.FromValue(metaDescriptionResult.Value);
                }
            }
        }

        // CategoryId (optional)
        if (categoryId.IsSet && categoryId.Value <= 0)
        {
            errors.Add(new ProductErrors.CategoryIdInvalid(categoryId.Value));
        }

        // Price (optional, nullable)
        Optional<Money?> priceValue = default;
        if (price.IsSet)
        {
            if (price.Value.HasValue)
            {
                var priceResult = Money.Create(price.Value.Value);
                if (priceResult.IsFailure)
                {
                    errors.Add(priceResult.Error);
                }
                else
                {
                    priceValue = Optional<Money?>.FromValue(priceResult.Value);
                }
            }
            else
            {
                priceValue = Optional<Money?>.FromValue(null);
            }
        }

        // Availability (optional)
        Optional<ProductAvailabilityStatus> availabilityValue = default;
        if (availability.IsSet)
        {
            var availabilityResult = ProductAvailabilityStatus.FromName(availability.Value);
            if (availabilityResult.IsFailure)
            {
                errors.Add(availabilityResult.Error);
            }
            else
            {
                availabilityValue = Optional<ProductAvailabilityStatus>.FromValue(availabilityResult.Value);
            }
        }

        // PublishStatus (optional)
        Optional<PublishStatus> publishStatusValue = default;
        if (publishStatus.IsSet)
        {
            var publishStatusResult = Domain.Catalog.PublishStatus.FromName(publishStatus.Value);
            if (publishStatusResult.IsFailure)
            {
                errors.Add(publishStatusResult.Error);
            }
            else
            {
                publishStatusValue = Optional<PublishStatus>.FromValue(publishStatusResult.Value);
            }
        }

        return Result<UpdateProductCommand>.FromValidation(
            errors,
            () => new UpdateProductCommand
            {
                ProductId = productId,
                RowVersion = rowVersion,
                Name = nameValue,
                SeoTitle = seoTitleValue,
                FullDescription = fullDescriptionValue,
                MetaDescription = metaDescriptionValue,
                CategoryId = categoryId,
                Price = priceValue,
                IsFeatured = isFeatured,
                Availability = availabilityValue,
                PublishStatus = publishStatusValue
            });
    }
}
