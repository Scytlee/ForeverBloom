using FluentValidation;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Categories.Queries.ListCategories;

internal sealed class ListCategoriesQueryValidator : AbstractValidator<ListCategoriesQuery>
{
    public ListCategoriesQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .MustBeValidPageNumber();

        RuleFor(query => query.PageSize)
            .MustBeValidPageSize();

        RuleForEach(query => query.SortBy)
            .MustBeValidSortProperty(ListCategoriesQuery.AllowedSortProperties)
            .When(query => query.SortBy is not null);

        RuleFor(query => query.SortBy!)
            .MustHaveNoDuplicateProperties()
            .When(query => query.SortBy is not null);
    }
}
