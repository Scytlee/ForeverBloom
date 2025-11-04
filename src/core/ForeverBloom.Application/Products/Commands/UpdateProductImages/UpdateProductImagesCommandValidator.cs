using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

public sealed class UpdateProductImagesCommandValidator : AbstractValidator<UpdateProductImagesCommand>
{
    public UpdateProductImagesCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .MustBeValidProductId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        RuleForEach(command => command.ImagesToCreate)
            .ChildRules(image =>
            {
                image.RuleFor(i => i)
                    .MustHaveValidImage(i => i.Source, i => i.AltText);
            });

        RuleForEach(command => command.ImagesToUpdate)
            .ChildRules(image =>
            {
                image.RuleFor(i => i.Id)
                    .MustBeValidImageId();

                image.RuleForOptional(i => i.AltText, altText =>
                {
                    altText.MustBeValidAltText();
                });
            });

        RuleForEach(command => command.ImagesToDelete)
            .MustBeValidImageId();

        RuleFor(command => command)
            .MustHaveUniqueImageIds()
            .MustRespectMaxImageCount();
    }
}
