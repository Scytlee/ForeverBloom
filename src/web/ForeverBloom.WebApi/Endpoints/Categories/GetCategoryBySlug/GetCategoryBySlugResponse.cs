using ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoryBySlug;

/// <summary>
/// Response payload returned for the public GetCategoryBySlug endpoint.
/// </summary>
internal sealed record GetCategoryBySlugResponse(
    long Id,
    string Name,
    string? Description,
    string Slug,
    string? ImageSource,
    string? ImageAltText,
    long? ParentCategoryId,
    IReadOnlyList<GetCategoryBySlugBreadcrumbItem> Breadcrumbs)
{
    internal static GetCategoryBySlugResponse FromResult(GetCategoryBySlugResult result)
    {
        var breadcrumbs = result.Breadcrumbs
            .Select(b => new GetCategoryBySlugBreadcrumbItem(
                Name: b.Name,
                Slug: b.Slug))
            .ToArray();

        return new GetCategoryBySlugResponse(
            Id: result.Id,
            Name: result.Name,
            Description: result.Description,
            Slug: result.Slug,
            ImageSource: result.ImageSource,
            ImageAltText: result.ImageAltText,
            ParentCategoryId: result.ParentCategoryId,
            Breadcrumbs: breadcrumbs);
    }
}

internal sealed record GetCategoryBySlugBreadcrumbItem(
    string Name,
    string Slug);
