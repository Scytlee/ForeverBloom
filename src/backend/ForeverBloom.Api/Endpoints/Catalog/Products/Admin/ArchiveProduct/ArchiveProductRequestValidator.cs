using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ArchiveProduct;

public sealed class ArchiveProductRequestValidator : AbstractValidator<ArchiveProductRequest>
{
    public ArchiveProductRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.RowVersionRequired);
    }
}
