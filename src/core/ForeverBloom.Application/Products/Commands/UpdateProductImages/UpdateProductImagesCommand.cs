using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

public sealed record UpdateProductImagesCommand : ICommand<UpdateProductImagesResult>
{
    public long ProductId { get; private init; }
    public uint RowVersion { get; private init; }
    public IReadOnlyList<CreateImageOperation> ImagesToCreate { get; private init; } = [];
    public IReadOnlyList<UpdateImageOperation> ImagesToUpdate { get; private init; } = [];
    public IReadOnlyList<long> ImagesToDelete { get; private init; } = [];

    private UpdateProductImagesCommand() { }

    public static Result<UpdateProductImagesCommand> Create(
        long productId,
        uint rowVersion,
        IReadOnlyList<CreateImageOperation>? imagesToCreate = null,
        IReadOnlyList<UpdateImageOperation>? imagesToUpdate = null,
        IReadOnlyList<long>? imagesToDelete = null)
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

        // Validate ImagesToCreate
        var validatedCreateImages = new List<CreateImageOperation>();
        if (imagesToCreate is { Count: > 0 })
        {
            foreach (var image in imagesToCreate)
            {
                var imageResult = Image.Create(image.Source, image.AltText);
                if (imageResult.IsFailure)
                {
                    if (imageResult.Error is CompositeError compositeError)
                    {
                        errors.AddRange(compositeError.Errors);
                    }
                    else
                    {
                        errors.Add(imageResult.Error);
                    }
                }
                else
                {
                    validatedCreateImages.Add(image);
                }
            }
        }

        // Validate ImagesToUpdate
        var validatedUpdateImages = new List<UpdateImageOperation>();
        if (imagesToUpdate is { Count: > 0 })
        {
            foreach (var image in imagesToUpdate)
            {
                // Image ID must be valid
                if (image.Id <= 0)
                {
                    errors.Add(new ProductErrors.ImageIdInvalid(image.Id));
                    continue;
                }

                // AltText (optional) - validate only if set and if it's a non-null value
                if (image.AltText.IsSet && image.AltText.Value is not null)
                {
                    if (image.AltText.Value.Length > Image.AltTextMaxLength)
                    {
                        errors.Add(new ImageErrors.AltTextTooLong(image.AltText.Value));
                    }
                }

                validatedUpdateImages.Add(image);
            }
        }

        // Validate ImagesToDelete
        var validatedDeleteImages = new List<long>();
        if (imagesToDelete is { Count: > 0 })
        {
            foreach (var imageId in imagesToDelete)
            {
                // Image ID must be valid
                if (imageId <= 0)
                {
                    errors.Add(new ProductErrors.ImageIdInvalid(imageId));
                }
                else
                {
                    validatedDeleteImages.Add(imageId);
                }
            }
        }

        // Validate unique image IDs (across delete and update)
        var allIds = validatedDeleteImages.Concat(validatedUpdateImages.Select(image => image.Id));
        var duplicateIds = allIds
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
        {
            errors.Add(new ProductErrors.DuplicateImageIds(duplicateIds));
        }

        // Validate max image count (total create + update operations)
        var totalOperations = validatedCreateImages.Count + validatedUpdateImages.Count;
        if (totalOperations > Product.MaxImageCount)
        {
            errors.Add(new ProductErrors.TooManyImages(totalOperations));
        }

        return Result<UpdateProductImagesCommand>.FromValidation(
            errors,
            () => new UpdateProductImagesCommand
            {
                ProductId = productId,
                RowVersion = rowVersion,
                ImagesToCreate = validatedCreateImages.AsReadOnly(),
                ImagesToUpdate = validatedUpdateImages.AsReadOnly(),
                ImagesToDelete = validatedDeleteImages.AsReadOnly()
            });
    }

    public sealed record CreateImageOperation(
        string Source,
        string? AltText,
        bool IsPrimary,
        int DisplayOrder);

    public sealed record UpdateImageOperation(
        long Id,
        Optional<string?> AltText,
        Optional<bool> IsPrimary,
        Optional<int> DisplayOrder);
}
