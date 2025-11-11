using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

public sealed record BrowseCatalogCategoryTreeQuery(
    long? RootCategoryId,
    int? Depth)
    : IQuery<BrowseCatalogCategoryTreeResult>;
