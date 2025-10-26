using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.NameRequired)
            .MaximumLength(CategoryValidation.Constants.NameMaxLength)
            .WithErrorCode(CategoryValidation.ErrorCodes.NameTooLong);

        RuleFor(x => x.Description)
            .MaximumLength(CategoryValidation.Constants.DescriptionMaxLength)
            .WithErrorCode(CategoryValidation.ErrorCodes.DescriptionTooLong)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.SlugRequired)
            .MaximumLength(SlugValidation.Constants.MaxLength)
            .WithErrorCode(CategoryValidation.ErrorCodes.SlugTooLong)
            .Matches(SlugValidation.Constants.Regex)
            .WithErrorCode(CategoryValidation.ErrorCodes.SlugInvalidFormat);

        RuleFor(x => x.ImagePath)
            .MaximumLength(CategoryValidation.Constants.ImagePathMaxLength)
            .WithErrorCode(CategoryValidation.ErrorCodes.ImagePathTooLong)
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePath));

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0)
            .WithErrorCode(CategoryValidation.ErrorCodes.ParentCategoryIdInvalid)
            .When(x => x.ParentCategoryId.HasValue);
    }
}
