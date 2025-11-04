using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Maps a <see cref="CreateCategoryCommand"/> into validated value objects consumed by the domain aggregate.
/// </summary>
internal static class CreateCategoryValueObjectsAssembler
{
    internal static Result<CreateCategoryValueObjects> AssembleValueObjects(
        this CreateCategoryCommand command)
    {
        var errors = new List<IError>();

        // Name (required)
        var nameResult = SeoTitle.Create(command.Name);
        if (nameResult.IsFailure)
        {
            errors.Add(nameResult.Error);
        }

        // Description (optional)
        Result<MetaDescription>? descriptionResult = null;
        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            descriptionResult = MetaDescription.Create(command.Description);
            if (descriptionResult.IsFailure)
            {
                errors.Add(descriptionResult.Error);
            }
        }

        // Slug (required)
        var slugResult = Slug.Create(command.Slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        // Image (optional)
        Result<Image>? imageResult = null;
        if (!string.IsNullOrWhiteSpace(command.ImagePath))
        {
            imageResult = Image.Create(command.ImagePath!, command.ImageAltText);
            if (imageResult.IsFailure)
            {
                errors.Add(imageResult.Error);
            }
        }

        return Result<CreateCategoryValueObjects>.FromValidation(
            errors,
            () => new CreateCategoryValueObjects(
                Name: nameResult.Value!,
                Description: descriptionResult?.Value,
                Slug: slugResult.Value!,
                Image: imageResult?.Value));
    }
}

/// <summary>
/// Value-object-typed inputs for creating a Category aggregate.
/// </summary>
internal sealed record CreateCategoryValueObjects(
    SeoTitle Name,
    MetaDescription? Description,
    Slug Slug,
    Image? Image
);
