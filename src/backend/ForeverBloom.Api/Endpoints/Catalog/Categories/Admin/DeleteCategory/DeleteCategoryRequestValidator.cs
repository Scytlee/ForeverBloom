using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.DeleteCategory;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public sealed class DeleteCategoryRequestValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.RowVersionRequired);
    }
}
