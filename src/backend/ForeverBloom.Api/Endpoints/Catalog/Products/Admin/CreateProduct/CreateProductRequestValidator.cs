using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.NameRequired)
            .MaximumLength(ProductValidation.Constants.NameMaxLength)
            .WithErrorCode(ProductValidation.ErrorCodes.NameTooLong);

        RuleFor(x => x.SeoTitle)
            .MaximumLength(ProductValidation.Constants.SeoTitleMaxLength)
            .WithErrorCode(ProductValidation.ErrorCodes.SeoTitleTooLong)
            .When(x => !string.IsNullOrWhiteSpace(x.SeoTitle));

        RuleFor(x => x.FullDescription)
            .MaximumLength(ProductValidation.Constants.FullDescriptionMaxLength)
            .WithErrorCode(ProductValidation.ErrorCodes.FullDescriptionTooLong)
            .When(x => !string.IsNullOrWhiteSpace(x.FullDescription));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(ProductValidation.Constants.MetaDescriptionMaxLength)
            .WithErrorCode(ProductValidation.ErrorCodes.MetaDescriptionTooLong)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.SlugRequired)
            .MaximumLength(SlugValidation.Constants.MaxLength)
            .WithErrorCode(ProductValidation.ErrorCodes.SlugTooLong)
            .Matches(SlugValidation.Constants.Regex)
            .WithErrorCode(ProductValidation.ErrorCodes.SlugInvalidFormat);

        RuleFor(x => x.Price)
            .InclusiveBetween(ProductValidation.Constants.MinPrice, ProductValidation.Constants.MaxPrice)
            .WithErrorCode(ProductValidation.ErrorCodes.PriceOutOfRange)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.CategoryIdRequired)
            .GreaterThan(0)
            .WithErrorCode(ProductValidation.ErrorCodes.CategoryIdRequired);

        RuleFor(x => x.PublishStatus)
            .IsInEnum()
            .WithErrorCode(ProductValidation.ErrorCodes.PublishStatusInvalid);

        RuleFor(x => x.Availability)
            .IsInEnum()
            .WithErrorCode(ProductValidation.ErrorCodes.AvailabilityStatusInvalid);
    }
}
