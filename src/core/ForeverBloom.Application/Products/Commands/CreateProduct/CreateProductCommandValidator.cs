using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValidProductName();

        RuleFor(x => x.SeoTitle)
            .MustBeValidSeoTitle()
            .When(x => !string.IsNullOrWhiteSpace(x.SeoTitle));

        RuleFor(x => x.MetaDescription)
            .MustBeValidMetaDescription()
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        RuleFor(x => x.Slug)
            .MustBeValidSlug();

        RuleFor(x => x.CategoryId)
            .MustBeValidCategoryId();

        RuleFor(x => x.Price)
            .MustBeValidMoney()
            .When(x => x.Price.HasValue);

        RuleFor(x => x.FullDescription)
            .MustBeValidHtmlFragment()
            .When(x => !string.IsNullOrWhiteSpace(x.FullDescription));

        RuleForEach(x => x.Images)
            .ChildRules(image =>
            {
                image.RuleFor(i => i)
                    .MustHaveValidImage(i => i.Source, i => i.AltText);
            })
            .When(x => x.Images is not null);
    }
}
