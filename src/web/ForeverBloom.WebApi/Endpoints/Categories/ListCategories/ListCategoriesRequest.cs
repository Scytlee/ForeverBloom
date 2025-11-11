using ForeverBloom.Application.Categories.Queries.ListCategories;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Categories.ListCategories;

internal sealed record ListCategoriesRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string? SortBy = null,
    [FromQuery] string? SearchTerm = null,
    [FromQuery] long? RootCategoryId = null,
    [FromQuery] bool? IncludeSubcategories = null,
    [FromQuery] string? PublishStatus = null)
{
    internal ListCategoriesQuery ToQuery(SortProperty[]? sortProperties, int? publishStatus)
    {
        return new ListCategoriesQuery
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortBy = sortProperties,
            SearchTerm = SearchTerm,
            RootCategoryId = RootCategoryId,
            IncludeSubcategories = IncludeSubcategories,
            PublishStatus = publishStatus
        };
    }
}
