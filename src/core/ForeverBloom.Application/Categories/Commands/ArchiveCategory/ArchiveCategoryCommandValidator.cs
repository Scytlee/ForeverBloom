using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.ArchiveCategory;

internal sealed class ArchiveCategoryCommandValidator
    : AbstractValidator<ArchiveCategoryCommand>
{
    public ArchiveCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(x => x.RowVersion)
            .MustBeValidRowVersion();
    }
}
