using FluentValidation;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Products.Queries.ListProducts;

internal sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .MustBeValidPageNumber();

        RuleFor(query => query.PageSize)
            .MustBeValidPageSize();

        RuleForEach(query => query.SortBy)
            .MustBeValidSortProperty(ListProductsQuery.AllowedSortProperties)
            .When(query => query.SortBy is not null);

        RuleFor(query => query.SortBy!)
            .MustHaveNoDuplicateProperties()
            .When(query => query.SortBy is not null);
    }
}
