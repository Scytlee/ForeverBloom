using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.RowVersionRequired);

        When(x => x.Name.IsSet, () =>
        {
            RuleFor(x => x.Name.Value)
                .NotEmpty()
                .WithErrorCode(CategoryValidation.ErrorCodes.NameRequired)
                .MaximumLength(CategoryValidation.Constants.NameMaxLength)
                .WithErrorCode(CategoryValidation.ErrorCodes.NameTooLong)
                .OverridePropertyName(nameof(UpdateCategoryRequest.Name));
        });

        When(x => x.Description.IsSet && !string.IsNullOrEmpty(x.Description.Value), () =>
        {
            RuleFor(x => x.Description.Value)
                .MaximumLength(CategoryValidation.Constants.DescriptionMaxLength)
                .WithErrorCode(CategoryValidation.ErrorCodes.DescriptionTooLong)
                .OverridePropertyName(nameof(UpdateCategoryRequest.Description));
        });

        When(x => x.Slug.IsSet, () =>
        {
            RuleFor(x => x.Slug.Value)
                .NotEmpty()
                .WithErrorCode(CategoryValidation.ErrorCodes.SlugRequired)
                .MaximumLength(SlugValidation.Constants.MaxLength)
                .WithErrorCode(CategoryValidation.ErrorCodes.SlugTooLong)
                .Matches(SlugValidation.Constants.Regex)
                .WithErrorCode(CategoryValidation.ErrorCodes.SlugInvalidFormat)
                .OverridePropertyName(nameof(UpdateCategoryRequest.Slug));
        });

        When(x => x.ImagePath.IsSet && !string.IsNullOrEmpty(x.ImagePath.Value), () =>
        {
            RuleFor(x => x.ImagePath.Value)
                .MaximumLength(CategoryValidation.Constants.ImagePathMaxLength)
                .WithErrorCode(CategoryValidation.ErrorCodes.ImagePathTooLong)
                .OverridePropertyName(nameof(UpdateCategoryRequest.ImagePath));
        });

        When(x => x.ParentCategoryId.IsSet, () =>
        {
            RuleFor(x => x.ParentCategoryId.Value)
                .GreaterThan(0)
                .WithErrorCode(CategoryValidation.ErrorCodes.ParentCategoryIdInvalid)
                .OverridePropertyName(nameof(UpdateCategoryRequest.ParentCategoryId));
        });
    }
}
