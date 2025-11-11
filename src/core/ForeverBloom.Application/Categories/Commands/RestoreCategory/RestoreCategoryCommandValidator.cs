using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.RestoreCategory;

internal sealed class RestoreCategoryCommandValidator
    : AbstractValidator<RestoreCategoryCommand>
{
    public RestoreCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(x => x.RowVersion)
            .MustBeValidRowVersion();
    }
}
