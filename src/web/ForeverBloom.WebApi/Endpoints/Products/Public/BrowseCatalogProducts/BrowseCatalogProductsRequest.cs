using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Products.Queries.BrowseCatalogProducts;
using ForeverBloom.SharedKernel.Result;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Products.Public.BrowseCatalogProducts;

internal sealed record BrowseCatalogProductsRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string Sort = "relevance",
    [FromQuery] long? CategoryId = null,
    [FromQuery] bool? Featured = null)
{
    internal Result<BrowseCatalogProductsQuery> ToQuery()
    {
        return BrowseCatalogProductsQuery.Create(
            PageNumber,
            PageSize,
            Sort,
            CategoryId,
            Featured);
    }
}
