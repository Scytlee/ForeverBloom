using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .MustBeValidProductId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();
    }
}
