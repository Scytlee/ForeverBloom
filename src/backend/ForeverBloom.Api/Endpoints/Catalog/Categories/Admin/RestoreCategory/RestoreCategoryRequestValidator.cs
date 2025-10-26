using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public sealed class RestoreCategoryRequestValidator : AbstractValidator<RestoreCategoryRequest>
{
    public RestoreCategoryRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.RowVersionRequired);
    }
}
