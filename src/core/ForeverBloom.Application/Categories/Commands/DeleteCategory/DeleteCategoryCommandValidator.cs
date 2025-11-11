using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();
    }
}
