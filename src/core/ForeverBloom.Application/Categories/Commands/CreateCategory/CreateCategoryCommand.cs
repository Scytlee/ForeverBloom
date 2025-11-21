using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Command to create a new category.
/// </summary>
public sealed record CreateCategoryCommand : ICommand<CreateCategoryResult>, IWithTransactionOverrides
{
    public SeoTitle Name { get; private init; } = null!;
    public MetaDescription? Description { get; private init; }
    public Slug Slug { get; private init; } = null!;
    public Image? Image { get; private init; }
    public long? ParentCategoryId { get; private init; }
    public int DisplayOrder { get; private init; }

    public TransactionSettings TransactionSettings { get; } = new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };

    private CreateCategoryCommand() { }

    public static Result<CreateCategoryCommand> Create(
        string name,
        string slug,
        string? description = null,
        string? imagePath = null,
        string? imageAltText = null,
        long? parentCategoryId = null,
        int displayOrder = 0)
    {
        var errors = new List<IError>();

        // Name (required)
        var nameResult = SeoTitle.Create(name);
        if (nameResult.IsFailure)
        {
            errors.Add(nameResult.Error);
        }

        // Description (optional)
        Result<MetaDescription>? descriptionResult = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            descriptionResult = MetaDescription.Create(description);
            if (descriptionResult.IsFailure)
            {
                errors.Add(descriptionResult.Error);
            }
        }

        // Slug (required)
        var slugResult = Slug.Create(slug);
        if (slugResult.IsFailure)
        {
            errors.Add(slugResult.Error);
        }

        // Image (optional)
        Result<Image>? imageResult = null;
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            imageResult = Image.Create(imagePath, imageAltText);
            if (imageResult.IsFailure)
            {
                errors.Add(imageResult.Error);
            }
        }

        // ParentCategoryId validation (optional but must be positive if provided)
        if (parentCategoryId is <= 0)
        {
            errors.Add(new CategoryErrors.ParentCategoryIdInvalid(parentCategoryId.Value));
        }

        return Result<CreateCategoryCommand>.FromValidation(
            errors,
            () => new CreateCategoryCommand
            {
                Name = nameResult.Value!,
                Description = descriptionResult?.Value,
                Slug = slugResult.Value!,
                Image = imageResult?.Value,
                ParentCategoryId = parentCategoryId,
                DisplayOrder = displayOrder
            });
    }
}
