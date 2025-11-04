using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

/// <summary>
/// Maps an UpdateCategoryCommand's primitives into value objects.
/// </summary>
internal static class UpdateCategoryValueObjectsAssembler
{
    internal static Result<UpdateCategoryValueObjects> AssembleValueObjects(this UpdateCategoryCommand command)
    {
        var errors = new List<IError>();

        // SeoTitle (optional)
        Optional<SeoTitle> name = default;
        if (command.Name.IsSet)
        {
            var nameResult = SeoTitle.Create(command.Name.Value);
            if (nameResult.IsFailure)
            {
                errors.Add(nameResult.Error);
            }
            else
            {
                name = Optional<SeoTitle>.FromValue(nameResult.Value);
            }
        }

        // MetaDescription (optional, nullable)
        Optional<MetaDescription?> description = default;
        if (command.Description.IsSet)
        {
            if (command.Description.Value is null)
            {
                description = Optional<MetaDescription?>.FromValue(null);
            }
            else
            {
                var descriptionResult = MetaDescription.Create(command.Description.Value);
                if (descriptionResult.IsFailure)
                {
                    errors.Add(descriptionResult.Error);
                }
                else
                {
                    description = Optional<MetaDescription?>.FromValue(descriptionResult.Value);
                }
            }
        }

        // Image (optional, nullable)
        Optional<Image?> image = default;
        if (command.ImagePath.IsSet)
        {
            if (command.ImagePath.Value is null)
            {
                image = Optional<Image?>.FromValue(null);
            }
            else
            {
                var imageResult = Image.Create(command.ImagePath.Value, command.ImageAltText.Value);
                if (imageResult.IsFailure)
                {
                    errors.Add(imageResult.Error);
                }
                else
                {
                    image = Optional<Image?>.FromValue(imageResult.Value);
                }
            }
        }

        return Result<UpdateCategoryValueObjects>.FromValidation(
            errors,
            () => new UpdateCategoryValueObjects(
                Name: name,
                Description: description,
                Image: image));
    }
}

/// <summary>
/// Value-object-typed inputs for updating Category.
/// </summary>
internal sealed record UpdateCategoryValueObjects(
    Optional<SeoTitle> Name,
    Optional<MetaDescription?> Description,
    Optional<Image?> Image
);
