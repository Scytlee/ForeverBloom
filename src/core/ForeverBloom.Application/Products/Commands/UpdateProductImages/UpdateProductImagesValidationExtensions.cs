using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Domain.Catalog;
using DomainProductErrors = ForeverBloom.Domain.Catalog.ProductErrors;

namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

/// <summary>
/// Validation extensions specific to UpdateProductImages use case.
/// </summary>
internal static class UpdateProductImagesValidationExtensions
{
    /// <summary>
    /// Validates that the command contains only unique image IDs among all inputs.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, UpdateProductImagesCommand> MustHaveUniqueImageIds<T>(
        this IRuleBuilder<T, UpdateProductImagesCommand> ruleBuilder)
    {
        return ruleBuilder.Custom((command, context) =>
        {
            var allIds = command.ImagesToDelete.Concat(command.ImagesToUpdate.Select(image => image.Id));

            var duplicateIds = allIds
                .GroupBy(id => id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            if (duplicateIds.Length > 0)
            {
                context.AddDuplicateImageIdsFailure(new ProductErrors.DuplicateImageIds(duplicateIds), duplicateIds);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for duplicate image IDs.
    /// </summary>
    private static void AddDuplicateImageIdsFailure<T>(
        this ValidationContext<T> context,
        ProductErrors.DuplicateImageIds error,
        IReadOnlyList<long> attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }

    /// <summary>
    /// Validates that the total number of images to create and update does not exceed the maximum allowed.
    /// </summary>
    internal static IRuleBuilderOptionsConditions<T, UpdateProductImagesCommand> MustRespectMaxImageCount<T>(
        this IRuleBuilder<T, UpdateProductImagesCommand> ruleBuilder)
    {
        return ruleBuilder.Custom((command, context) =>
        {
            var totalOperations = command.ImagesToCreate.Count + command.ImagesToUpdate.Count;
            if (totalOperations > Product.MaxImageCount)
            {
                context.AddTooManyImagesFailure(new DomainProductErrors.TooManyImages(totalOperations), totalOperations);
            }
        });
    }

    /// <summary>
    /// Adds a validation failure for too many images.
    /// </summary>
    private static void AddTooManyImagesFailure<T>(
        this ValidationContext<T> context,
        DomainProductErrors.TooManyImages error,
        int attemptedValue)
    {
        var failure = ValidationExtensions.CreateFailureFromError(context, error, attemptedValue);
        context.AddFailure(failure);
    }
}
