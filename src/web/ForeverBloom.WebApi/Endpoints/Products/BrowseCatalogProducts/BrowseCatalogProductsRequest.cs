using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;
using ForeverBloom.Application.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Products.BrowseCatalogProducts;

internal sealed record BrowseCatalogProductsRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string? Sort = null,
    [FromQuery] long? CategoryId = null,
    [FromQuery] bool? Featured = null)
{
    internal BrowseCatalogProductsQuery ToQuery()
    {
        return new BrowseCatalogProductsQuery
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortStrategy = new SortStrategy(string.IsNullOrWhiteSpace(Sort) ? "relevance" : Sort),
            CategoryId = CategoryId,
            Featured = Featured
        };
    }
}
