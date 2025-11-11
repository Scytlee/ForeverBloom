using FluentValidation;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;

internal sealed class BrowseCatalogProductsQueryValidator : AbstractValidator<BrowseCatalogProductsQuery>
{
    public BrowseCatalogProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .MustBeValidPageNumber();

        RuleFor(query => query.PageSize)
            .MustBeValidPageSize();

        RuleFor(query => query.SortStrategy)
            .MustBeValidSortStrategy(BrowseCatalogProductsQuery.AllowedSortStrategies);
    }
}
