using FluentValidation;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.DeleteProduct;
using ForeverBloom.Domain.Shared.Validation;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.DeleteProduct;

public sealed class DeleteProductRequestValidator : AbstractValidator<DeleteProductRequest>
{
    public DeleteProductRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithErrorCode(ProductValidation.ErrorCodes.RowVersionRequired);
    }
}
