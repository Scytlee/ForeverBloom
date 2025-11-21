using ForeverBloom.Application.Categories.Queries.ListCategories;
using ForeverBloom.Application.Pagination;
using ForeverBloom.SharedKernel.Result;
using Microsoft.AspNetCore.Mvc;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ListCategories;

internal sealed record ListCategoriesRequest(
    [FromQuery] int PageNumber = PaginationConstants.DefaultPageNumber,
    [FromQuery] int PageSize = PaginationConstants.DefaultPageSize,
    [FromQuery] string? SortBy = null,
    [FromQuery] string? SearchTerm = null,
    [FromQuery] long? RootCategoryId = null,
    [FromQuery] bool? IncludeSubcategories = null,
    [FromQuery] string? PublishStatus = null)
{
    internal Result<ListCategoriesQuery> ToQuery()
    {
        return ListCategoriesQuery.Create(
            pageNumber: PageNumber,
            pageSize: PageSize,
            sortBy: SortBy,
            searchTerm: SearchTerm,
            rootCategoryId: RootCategoryId,
            includeSubcategories: IncludeSubcategories,
            publishStatus: PublishStatus);
    }
}
