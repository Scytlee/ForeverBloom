using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.ReslugProduct;

public sealed class ReslugProductCommandValidator : AbstractValidator<ReslugProductCommand>
{
    public ReslugProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .MustBeValidProductId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        RuleFor(command => command.NewSlug)
            .MustBeValidSlug();
    }
}
