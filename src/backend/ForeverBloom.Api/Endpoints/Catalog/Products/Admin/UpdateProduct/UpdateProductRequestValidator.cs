using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProduct;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.RowVersionRequired);

        When(x => x.Name.IsSet, () =>
        {
            RuleFor(x => x.Name.Value)
                .NotEmpty()
                .WithErrorCode(ProductValidation.ErrorCodes.NameRequired)
                .MaximumLength(ProductValidation.Constants.NameMaxLength)
                .WithErrorCode(ProductValidation.ErrorCodes.NameTooLong)
                .OverridePropertyName(nameof(UpdateProductRequest.Name));
        });

        When(x => x.SeoTitle.IsSet, () =>
        {
            RuleFor(x => x.SeoTitle.Value)
                .MaximumLength(ProductValidation.Constants.SeoTitleMaxLength)
                .WithErrorCode(ProductValidation.ErrorCodes.SeoTitleTooLong)
                .OverridePropertyName(nameof(UpdateProductRequest.SeoTitle));
        });

        When(x => x.FullDescription.IsSet, () =>
        {
            RuleFor(x => x.FullDescription.Value)
                .MaximumLength(ProductValidation.Constants.FullDescriptionMaxLength)
                .WithErrorCode(ProductValidation.ErrorCodes.FullDescriptionTooLong)
                .OverridePropertyName(nameof(UpdateProductRequest.FullDescription));
        });

        When(x => x.MetaDescription.IsSet, () =>
        {
            RuleFor(x => x.MetaDescription.Value)
                .MaximumLength(ProductValidation.Constants.MetaDescriptionMaxLength)
                .WithErrorCode(ProductValidation.ErrorCodes.MetaDescriptionTooLong)
                .OverridePropertyName(nameof(UpdateProductRequest.MetaDescription));
        });

        When(x => x.Slug.IsSet, () =>
        {
            RuleFor(x => x.Slug.Value)
                .NotEmpty()
                .WithErrorCode(ProductValidation.ErrorCodes.SlugRequired)
                .MaximumLength(SlugValidation.Constants.MaxLength)
                .WithErrorCode(ProductValidation.ErrorCodes.SlugTooLong)
                .Matches(SlugValidation.Constants.Regex)
                .WithErrorCode(ProductValidation.ErrorCodes.SlugInvalidFormat)
                .OverridePropertyName(nameof(UpdateProductRequest.Slug));
        });

        When(x => x.Price.IsSet, () =>
        {
            RuleFor(x => x.Price.Value)
                .InclusiveBetween(ProductValidation.Constants.MinPrice, ProductValidation.Constants.MaxPrice)
                .When(x => x.Price.Value.HasValue)
                .WithErrorCode(ProductValidation.ErrorCodes.PriceOutOfRange)
                .OverridePropertyName(nameof(UpdateProductRequest.Price));
        });

        When(x => x.CategoryId.IsSet, () =>
        {
            RuleFor(x => x.CategoryId.Value)
                .GreaterThan(0)
                .WithErrorCode(ProductValidation.ErrorCodes.CategoryIdRequired)
                .OverridePropertyName(nameof(UpdateProductRequest.CategoryId));
        });

        When(x => x.PublishStatus.IsSet, () =>
        {
            RuleFor(x => x.PublishStatus.Value)
                .IsInEnum()
                .WithErrorCode(ProductValidation.ErrorCodes.PublishStatusInvalid)
                .OverridePropertyName(nameof(UpdateProductRequest.PublishStatus));
        });

        When(x => x.Availability.IsSet, () =>
        {
            RuleFor(x => x.Availability.Value)
                .IsInEnum()
                .WithErrorCode(ProductValidation.ErrorCodes.AvailabilityStatusInvalid)
                .OverridePropertyName(nameof(UpdateProductRequest.Availability));
        });
    }
}
