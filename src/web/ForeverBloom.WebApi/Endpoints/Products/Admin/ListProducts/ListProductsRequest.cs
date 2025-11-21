using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.ListProducts;
using ForeverBloom.SharedKernel.Result;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ListProducts;

internal sealed record ListProductsRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string? SortBy = null,
    [FromQuery] string? SearchTerm = null,
    [FromQuery] long? CategoryId = null,
    [FromQuery] bool? IncludeSubcategories = null)
{
    internal Result<ListProductsQuery> ToQuery()
    {
        return ListProductsQuery.Create(
            PageNumber,
            PageSize,
            SortBy,
            SearchTerm,
            CategoryId,
            IncludeSubcategories);
    }
}
