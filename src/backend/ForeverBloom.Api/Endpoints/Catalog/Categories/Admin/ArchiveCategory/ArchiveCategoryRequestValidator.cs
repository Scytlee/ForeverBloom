using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public sealed class ArchiveCategoryRequestValidator : AbstractValidator<ArchiveCategoryRequest>
{
    public ArchiveCategoryRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(CategoryValidation.ErrorCodes.RowVersionRequired);
    }
}
