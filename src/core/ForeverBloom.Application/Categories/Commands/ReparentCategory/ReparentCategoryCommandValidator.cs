using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.ReparentCategory;

public sealed class ReparentCategoryCommandValidator : AbstractValidator<ReparentCategoryCommand>
{
    public ReparentCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        RuleFor(command => command.NewParentCategoryId)
            .MustBeValidParentCategoryId()
            .When(command => command.NewParentCategoryId.HasValue);

        RuleFor(command => command.NewParentCategoryId)
            .MustNotBeOwnParent(command => command.CategoryId);
    }
}
