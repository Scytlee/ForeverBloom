using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.RestoreProduct;

public sealed class RestoreProductCommandValidator : AbstractValidator<RestoreProductCommand>
{
    public RestoreProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .MustBeValidProductId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();
    }
}
