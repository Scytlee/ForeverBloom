using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.RestoreProduct;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.RestoreProduct;

public sealed class RestoreProductRequestValidator : AbstractValidator<RestoreProductRequest>
{
    public RestoreProductRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.RowVersionRequired);
    }
}
