using FluentValidation;
using ForeverBloom.Application.Abstractions.Validation;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .MustBeValidProductId();

        RuleFor(command => command.RowVersion)
            .MustBeValidRowVersion();

        this.RuleForOptional(command => command.Name, name =>
        {
            name.MustBeValidProductName();
        });

        this.RuleForOptional(command => command.SeoTitle, seoTitle =>
        {
            seoTitle.MustBeValidSeoTitle();
        });

        this.RuleForOptional(command => command.FullDescription, fullDescription =>
        {
            fullDescription.MustBeValidHtmlFragment();
        });

        this.RuleForOptional(command => command.MetaDescription, metaDescription =>
        {
            metaDescription.MustBeValidMetaDescription();
        });

        this.RuleForOptional(command => command.CategoryId, categoryId =>
        {
            categoryId.MustBeValidCategoryId();
        });

        this.RuleForOptional(command => command.Price, price =>
        {
            price.MustBeValidMoney();
        });
    }
}
