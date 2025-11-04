using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValidSeoTitle();

        RuleFor(x => x.Description)
            .MustBeValidMetaDescription()
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Slug)
            .MustBeValidSlug();

        RuleFor(x => x)
            .MustHaveValidImage(x => x.ImagePath!, x => x.ImageAltText)
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePath));

        RuleFor(x => x.ParentCategoryId)
            .MustBeValidParentCategoryId()
            .When(x => x.ParentCategoryId.HasValue);
    }
}
