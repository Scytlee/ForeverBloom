using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

/// <summary>
/// Command to update an existing category.
/// </summary>
public sealed record UpdateCategoryCommand : ICommand<UpdateCategoryResult>
{
    public long CategoryId { get; private init; }
    public uint RowVersion { get; private init; }
    public Optional<SeoTitle> Name { get; private init; }
    public Optional<MetaDescription?> Description { get; private init; }
    public Optional<Image?> Image { get; private init; }
    public Optional<int> DisplayOrder { get; private init; }
    public Optional<PublishStatus> PublishStatus { get; private init; }

    private UpdateCategoryCommand() { }

    public static Result<UpdateCategoryCommand> Create(
        long categoryId,
        uint rowVersion,
        Optional<string> name = default,
        Optional<string?> description = default,
        Optional<string?> imagePath = default,
        Optional<string?> imageAltText = default,
        Optional<int> displayOrder = default,
        Optional<string> publishStatus = default)
    {
        var errors = new List<IError>();

        // CategoryId (required)
        if (categoryId <= 0)
        {
            errors.Add(new CategoryErrors.CategoryIdInvalid(categoryId));
        }

        // RowVersion (required)
        if (rowVersion <= 0)
        {
            errors.Add(new ApplicationErrors.RowVersionInvalid(rowVersion));
        }

        // Name (optional)
        Optional<SeoTitle> nameValue = default;
        if (name.IsSet)
        {
            var nameResult = SeoTitle.Create(name.Value);
            if (nameResult.IsFailure)
            {
                errors.Add(nameResult.Error);
            }
            else
            {
                nameValue = Optional<SeoTitle>.FromValue(nameResult.Value);
            }
        }

        // Description (optional, nullable)
        Optional<MetaDescription?> descriptionValue = default;
        if (description.IsSet)
        {
            if (description.Value is null)
            {
                descriptionValue = Optional<MetaDescription?>.FromValue(null);
            }
            else
            {
                var descriptionResult = MetaDescription.Create(description.Value);
                if (descriptionResult.IsFailure)
                {
                    errors.Add(descriptionResult.Error);
                }
                else
                {
                    descriptionValue = Optional<MetaDescription?>.FromValue(descriptionResult.Value);
                }
            }
        }

        // Image (optional, nullable)
        // When updating an image, both path and alt text must be provided together
        Optional<Image?> imageValue = default;

        // Validate that image path and alt text are provided together
        if (imagePath is { IsSet: true, Value: not null } && !imageAltText.IsSet)
        {
            errors.Add(new ImageErrors.CannotUpdatePartially());
        }
        else if (imageAltText is { IsSet: true, Value: not null } && !imagePath.IsSet)
        {
            errors.Add(new ImageErrors.CannotUpdatePartially());
        }
        else if (imagePath.IsSet)
        {
            if (imagePath.Value is null)
            {
                // Removing the image (alt text is ignored)
                imageValue = Optional<Image?>.FromValue(null);
            }
            else
            {
                // Both path and alt text are provided, create/update image
                var imageResult = Domain.Catalog.Image.Create(imagePath.Value, imageAltText.Value);
                if (imageResult.IsFailure)
                {
                    errors.Add(imageResult.Error);
                }
                else
                {
                    imageValue = Optional<Image?>.FromValue(imageResult.Value);
                }
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

        return Result<UpdateCategoryCommand>.FromValidation(
            errors,
            () => new UpdateCategoryCommand
            {
                CategoryId = categoryId,
                RowVersion = rowVersion,
                Name = nameValue,
                Description = descriptionValue,
                Image = imageValue,
                DisplayOrder = displayOrder,
                PublishStatus = publishStatusValue
            });
    }
}
