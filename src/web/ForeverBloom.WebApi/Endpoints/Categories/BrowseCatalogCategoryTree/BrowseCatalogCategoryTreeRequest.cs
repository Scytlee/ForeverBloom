using ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Categories.BrowseCatalogCategoryTree;

internal sealed record BrowseCatalogCategoryTreeRequest(
    [FromQuery] long? RootCategoryId = null,
    [FromQuery] int? Depth = null)
{
    internal BrowseCatalogCategoryTreeQuery ToQuery()
    {
        return new BrowseCatalogCategoryTreeQuery(RootCategoryId, Depth);
    }
}
