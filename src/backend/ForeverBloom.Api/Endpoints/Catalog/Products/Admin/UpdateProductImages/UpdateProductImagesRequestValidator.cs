using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public sealed class UpdateProductImagesRequestValidator : AbstractValidator<UpdateProductImagesRequest>
{
    public UpdateProductImagesRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.RowVersionRequired);

        RuleFor(x => x.Images)
            .NotNull()
            .WithErrorCode(ProductValidation.ErrorCodes.NoImageCollectionProvided);

        // If images exist, ensure exactly one is marked as primary
        When(x => x.Images != null && x.Images.Any(), () =>
        {
            RuleFor(x => x.Images)
                .Must(images => images.Count(i => i.IsPrimary) == 1)
                .WithErrorCode(ProductValidation.ErrorCodes.ExactlyOnePrimaryImage);
        });

        // Validate display order uniqueness (Product-level concern)
        When(x => x.Images != null && x.Images.Any(), () =>
        {
            RuleFor(x => x.Images)
                .Must(images => images.DistinctBy(i => i.DisplayOrder).Count() == images.Count)
                .WithErrorCode(ProductValidation.ErrorCodes.DuplicateImageDisplayOrder);
        });

        // Validate individual image items
        RuleForEach(x => x.Images)
            .SetValidator(new UpdateProductImageItemValidator());
    }
}

public sealed class UpdateProductImageItemValidator : AbstractValidator<UpdateProductImageItem>
{
    public UpdateProductImageItemValidator()
    {
        RuleFor(x => x.ImagePath)
            .NotEmpty()
            .WithErrorCode(ProductImageValidation.ErrorCodes.ImagePathRequired)
            .MaximumLength(ProductImageValidation.Constants.ImagePathMaxLength)
            .WithErrorCode(ProductImageValidation.ErrorCodes.ImagePathTooLong);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(ProductImageValidation.ErrorCodes.InvalidDisplayOrder);

        When(x => x.AltText != null, () =>
        {
            RuleFor(x => x.AltText)
                .MaximumLength(ProductImageValidation.Constants.AltTextMaxLength)
                .WithErrorCode(ProductImageValidation.ErrorCodes.AltTextTooLong);
        });
    }
}
