using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ListAdminCategories;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ListAdminCategories;

public static class ListAdminCategoriesEndpoint
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;

    private static readonly HashSet<string> AllowedSortColumns = SortingHelper.CreateAllowedSortColumns(
        nameof(AdminCategoryListItem.Name),
        nameof(AdminCategoryListItem.DisplayOrder),
        nameof(AdminCategoryListItem.Id)
    );

    public static IServiceCollection AddListAdminCategoriesEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IListAdminCategoriesEndpointQueryProvider, ListAdminCategoriesEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapListAdminCategoriesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(CategoryEndpointsGroup.Names.ListAdminCategories);

        return app;
    }

    internal static async Task<Results<OkResult<ListAdminCategoriesResponse>, ValidationProblemResult>> HandleAsync(
        [AsParameters] ListAdminCategoriesRequest request,
        IListAdminCategoriesEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        request.PageNumber ??= DefaultPageNumber;
        request.PageSize ??= DefaultPageSize;

        // Validate sort parameters
        if (!SortingHelper.TryParseAndValidateSortString(request.OrderBy, AllowedSortColumns, out var sortColumns))
        {
            return ApiResults.ValidationProblem(nameof(request.OrderBy), CategoryValidation.ErrorCodes.InvalidSortParameters);
        }

        var categories = await queryProvider.GetCategoriesAsync(request, sortColumns, cancellationToken);
        var totalCount = await queryProvider.GetCategoriesCountAsync(request, cancellationToken);

        var response = new ListAdminCategoriesResponse(categories, request.PageNumber.Value, request.PageSize.Value, totalCount);

        return ApiResults.Ok(response);
    }
}
