using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.ListProducts;
using ForeverBloom.Application.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Products.ListProducts;

internal sealed record ListProductsRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string? SortBy = null,
    [FromQuery] string? SearchTerm = null,
    [FromQuery] long? CategoryId = null,
    [FromQuery] bool? IncludeSubcategories = null)
{
    internal ListProductsQuery ToQuery(SortProperty[]? sortProperties)
    {
        return new ListProductsQuery
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortBy = sortProperties,
            SearchTerm = SearchTerm,
            CategoryId = CategoryId,
            IncludeSubcategories = IncludeSubcategories
        };
    }
}
