using ForeverBloom.Application.Categories.Queries.ListCategories;
using ForeverBloom.WebApi.Mapping;

namespace ForeverBloom.WebApi.Endpoints.Categories.ListCategories;

internal sealed record ListCategoriesResponse(
    IReadOnlyList<ListCategoriesResponseItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    internal static ListCategoriesResponse FromResult(ListCategoriesResult result)
    {
        var items = result.Items
            .Select(item => new ListCategoriesResponseItem(
                Id: item.Id,
                Name: item.Name,
                Slug: item.Slug,
                Description: item.Description,
                ParentCategoryId: item.ParentCategoryId,
                Path: item.Path,
                DisplayOrder: item.DisplayOrder,
                PublishStatus: PublishStatusMapper.ToString(item.PublishStatus),
                ImageSource: item.ImageSource,
                ImageAltText: item.ImageAltText,
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt,
                DeletedAt: item.DeletedAt))
            .ToArray();

        return new ListCategoriesResponse(
            Items: items,
            PageNumber: result.PageNumber,
            PageSize: result.PageSize,
            TotalCount: result.TotalCount,
            TotalPages: result.TotalPages);
    }
}

internal sealed record ListCategoriesResponseItem(
    long Id,
    string Name,
    string Slug,
    string? Description,
    long? ParentCategoryId,
    string? Path,
    int DisplayOrder,
    string PublishStatus,
    string? ImageSource,
    string? ImageAltText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);
