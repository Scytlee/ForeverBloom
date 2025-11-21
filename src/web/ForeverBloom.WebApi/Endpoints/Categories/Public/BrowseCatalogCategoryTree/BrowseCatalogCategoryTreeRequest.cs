using ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;
using ForeverBloom.SharedKernel.Result;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Categories.Public.BrowseCatalogCategoryTree;

internal sealed record BrowseCatalogCategoryTreeRequest(
    [FromQuery] long? RootCategoryId = null,
    [FromQuery] int? Levels = null)
{
    internal Result<BrowseCatalogCategoryTreeQuery> ToQuery() =>
        BrowseCatalogCategoryTreeQuery.Create(RootCategoryId, Levels);
}
