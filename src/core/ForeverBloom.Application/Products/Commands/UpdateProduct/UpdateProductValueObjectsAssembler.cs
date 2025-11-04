using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Maps an UpdateProductCommand's primitives into value objects.
/// </summary>
internal static class UpdateProductValueObjectsAssembler
{
    internal static Result<UpdateProductValueObjects> AssembleValueObjects(this UpdateProductCommand command)
    {
        var errors = new List<IError>();

        // ProductName (optional)
        Optional<ProductName> name = default;
        if (command.Name.IsSet)
        {
            var nameResult = ProductName.Create(command.Name.Value);
            if (nameResult.IsFailure)
            {
                errors.Add(nameResult.Error);
            }
            else
            {
                name = Optional<ProductName>.FromValue(nameResult.Value);
            }
        }

        // SeoTitle (optional, nullable)
        Optional<SeoTitle?> seoTitle = default;
        if (command.SeoTitle.IsSet)
        {
            if (command.SeoTitle.Value is null)
            {
                seoTitle = Optional<SeoTitle?>.FromValue(null);
            }
            else
            {
                var seoTitleResult = SeoTitle.Create(command.SeoTitle.Value);
                if (seoTitleResult.IsFailure)
                {
                    errors.Add(seoTitleResult.Error);
                }
                else
                {
                    seoTitle = Optional<SeoTitle?>.FromValue(seoTitleResult.Value);
                }
            }
        }

        // FullDescription (optional, nullable)
        Optional<HtmlFragment?> fullDescription = default;
        if (command.FullDescription.IsSet)
        {
            if (command.FullDescription.Value is null)
            {
                fullDescription = Optional<HtmlFragment?>.FromValue(null);
            }
            else
            {
                var fullDescriptionResult = HtmlFragment.Create(command.FullDescription.Value);
                if (fullDescriptionResult.IsFailure)
                {
                    errors.Add(fullDescriptionResult.Error);
                }
                else
                {
                    fullDescription = Optional<HtmlFragment?>.FromValue(fullDescriptionResult.Value);
                }
            }
        }

        // MetaDescription (optional, nullable)
        Optional<MetaDescription?> metaDescription = default;
        if (command.MetaDescription.IsSet)
        {
            if (command.MetaDescription.Value is null)
            {
                metaDescription = Optional<MetaDescription?>.FromValue(null);
            }
            else
            {
                var metaDescriptionResult = MetaDescription.Create(command.MetaDescription.Value);
                if (metaDescriptionResult.IsFailure)
                {
                    errors.Add(metaDescriptionResult.Error);
                }
                else
                {
                    metaDescription = Optional<MetaDescription?>.FromValue(metaDescriptionResult.Value);
                }
            }
        }

        // Price (optional, nullable)
        Optional<Money?> price = default;
        if (command.Price.IsSet)
        {
            if (command.Price.Value.HasValue)
            {
                var priceResult = Money.Create(command.Price.Value.Value);
                if (priceResult.IsFailure)
                {
                    errors.Add(priceResult.Error);
                }
                else
                {
                    price = Optional<Money?>.FromValue(priceResult.Value);
                }
            }
            else
            {
                price = Optional<Money?>.FromValue(null);
            }
        }

        return Result<UpdateProductValueObjects>.FromValidation(
            errors,
            () => new UpdateProductValueObjects(
                Name: name,
                SeoTitle: seoTitle,
                FullDescription: fullDescription,
                MetaDescription: metaDescription,
                Price: price));
    }
}

/// <summary>
/// Value-object-typed inputs for updating Product.
/// </summary>
internal sealed record UpdateProductValueObjects(
    Optional<ProductName> Name,
    Optional<SeoTitle?> SeoTitle,
    Optional<HtmlFragment?> FullDescription,
    Optional<MetaDescription?> MetaDescription,
    Optional<Money?> Price
);
