using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        this.RuleForOptional(command => command.Name, name =>
        {
            name.MustBeValidSeoTitle();
        });

        this.RuleForOptional(command => command.Description, description =>
        {
            description.MustBeValidMetaDescription();
        });

        RuleFor(command => command)
            .MustHaveValidImage(
                x => x.ImagePath!,
                x => x.ImageAltText)
            .When(x => x.ImagePath.IsSet && !string.IsNullOrWhiteSpace(x.ImagePath.Value));
    }
}
