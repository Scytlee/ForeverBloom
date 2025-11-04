using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.ReslugCategory;

public sealed class ReslugCategoryCommandValidator : AbstractValidator<ReslugCategoryCommand>
{
    public ReslugCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        RuleFor(command => command.NewSlug)
            .MustBeValidSlug();
    }
}
