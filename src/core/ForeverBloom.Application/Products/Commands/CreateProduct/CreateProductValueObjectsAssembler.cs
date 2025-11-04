using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.CreateProduct;

/// <summary>
/// Maps a CreateProductCommand's primitives into value objects.
/// </summary>
internal static class CreateProductValueObjectsAssembler
{
    internal static Result<CreateProductValueObjects> AssembleValueObjects(this CreateProductCommand command)
    {
        var errors = new List<IError>();

        // ProductName (required)
        var nameResult = ProductName.Create(command.Name);
        if (nameResult.IsFailure)
        {
            errors.Add(nameResult.Error);
        }

        // Slug (required)
        var slugResult = Slug.Create(command.Slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        // SeoTitle (optional)
        Result<SeoTitle>? seoTitleResult = null;
        if (!string.IsNullOrWhiteSpace(command.SeoTitle))
        {
            seoTitleResult = SeoTitle.Create(command.SeoTitle);
            if (seoTitleResult.IsFailure)
            {
                errors.Add(seoTitleResult.Error);
            }
        }

        // MetaDescription (optional)
        Result<MetaDescription>? metaDescriptionResult = null;
        if (!string.IsNullOrWhiteSpace(command.MetaDescription))
        {
            metaDescriptionResult = MetaDescription.Create(command.MetaDescription);
            if (metaDescriptionResult.IsFailure)
            {
                errors.Add(metaDescriptionResult.Error);
            }
        }

        // FullDescription (optional)
        Result<HtmlFragment>? fullDescriptionResult = null;
        if (!string.IsNullOrWhiteSpace(command.FullDescription))
        {
            fullDescriptionResult = HtmlFragment.Create(command.FullDescription);
            if (fullDescriptionResult.IsFailure)
            {
                errors.Add(fullDescriptionResult.Error);
            }
        }

        // Price (optional)
        Result<Money>? priceResult = null;
        if (command.Price.HasValue)
        {
            priceResult = Money.Create(command.Price.Value);
            if (priceResult.IsFailure)
            {
                errors.Add(priceResult.Error);
            }
        }

        // Images (optional collection)
        ProductImage[] images = [];
        if (command.Images is { Count: > 0 } imageInputs)
        {
            var imageValueObjects = new List<ProductImage>(imageInputs.Count);

            foreach (var imageInput in imageInputs)
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

            if (imageValueObjects.Count > 0)
            {
                images = imageValueObjects.ToArray();
            }
        }

        return Result<CreateProductValueObjects>.FromValidation(
            errors,
            () => new CreateProductValueObjects(
                Name: nameResult.Value!,
                SeoTitle: seoTitleResult?.Value,
                MetaDescription: metaDescriptionResult?.Value,
                FullDescription: fullDescriptionResult?.Value,
                Slug: slugResult.Value!,
                Price: priceResult?.Value,
                Images: images));
    }
}

/// <summary>
/// Value-object-typed inputs for creating a Product aggregate.
/// </summary>
internal sealed record CreateProductValueObjects(
    ProductName Name,
    SeoTitle? SeoTitle,
    MetaDescription? MetaDescription,
    HtmlFragment? FullDescription,
    Slug Slug,
    Money? Price,
    ProductImage[] Images
);
