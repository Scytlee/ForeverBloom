namespace ForeverBloom.WebApi.Client.Endpoints.Categories.Contracts;

/// <summary>
/// Response containing full category details with breadcrumbs.
/// </summary>
public sealed record GetCategoryBySlugResponse(
    long Id,
    string Name,
    string? Description,
    string Slug,
    string? ImageSource,
    string? ImageAltText,
    long? ParentCategoryId,
    IReadOnlyList<GetCategoryBySlugBreadcrumbItem> Breadcrumbs);

/// <summary>
/// Breadcrumb item for category navigation.
/// </summary>
public sealed record GetCategoryBySlugBreadcrumbItem(
    string Name,
    string Slug);
