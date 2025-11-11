using FluentValidation;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

internal sealed class BrowseCatalogCategoryTreeQueryValidator : AbstractValidator<BrowseCatalogCategoryTreeQuery>
{
    public BrowseCatalogCategoryTreeQueryValidator()
    {
        RuleFor(query => query.RootCategoryId)
            .MustBeValidCategoryId()
            .When(query => query.RootCategoryId.HasValue);

        RuleFor(query => query.Depth)
            .MustBeValidDepth()
            .When(query => query.Depth.HasValue);
    }
}
