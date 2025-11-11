using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Pagination;
using ForeverBloom.Application.Sorting;

namespace ForeverBloom.Application.Categories.Queries.ListCategories;

public sealed record ListCategoriesQuery : IQuery<ListCategoriesResult>
{
    public static readonly HashSet<string> AllowedSortProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "created_at", "updated_at"
    };

    public int PageNumber { get; init; } = PaginationConstants.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationConstants.DefaultPageSize;
    public SortProperty[]? SortBy { get; init; }
    public string? SearchTerm { get; init; }
    public long? RootCategoryId { get; init; }
    public bool? IncludeSubcategories { get; init; }
    public int? PublishStatus { get; init; }
}
