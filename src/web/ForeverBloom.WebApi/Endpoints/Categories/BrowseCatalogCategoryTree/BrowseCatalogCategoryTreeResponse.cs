using ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

namespace ForeverBloom.WebApi.Endpoints.Categories.BrowseCatalogCategoryTree;

internal sealed record BrowseCatalogCategoryTreeResponse(
    IReadOnlyList<BrowseCatalogCategoryTreeResponseItem> Categories)
{
    internal static BrowseCatalogCategoryTreeResponse FromResult(BrowseCatalogCategoryTreeResult result)
    {
        return new BrowseCatalogCategoryTreeResponse(
            Categories: result.Categories
                .Select(BrowseCatalogCategoryTreeResponseItem.FromResult)
                .ToArray());
    }
}

internal sealed record BrowseCatalogCategoryTreeResponseItem(
    long Id,
    string Name,
    string Slug,
    string? ImageSource,
    string? ImageAltText,
    IReadOnlyList<BrowseCatalogCategoryTreeResponseItem> Children)
{
    internal static BrowseCatalogCategoryTreeResponseItem FromResult(BrowseCatalogCategoryTreeResultItem item)
    {
        return new BrowseCatalogCategoryTreeResponseItem(
            Id: item.Id,
            Name: item.Name,
            Slug: item.Slug,
            ImageSource: item.ImageSource,
            ImageAltText: item.ImageAltText,
            Children: item.Children.Select(FromResult).ToArray());
    }
}
