using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ListAdminProducts;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ListAdminProducts;

public static class ListAdminProductsEndpoint
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;

    private static readonly HashSet<string> AllowedSortColumns = SortingHelper.CreateAllowedSortColumns(
        nameof(ProductListItem.Name),
        nameof(ProductListItem.DisplayOrder),
        nameof(ProductListItem.Price),
        nameof(ProductListItem.CreatedAt),
        nameof(ProductListItem.CategoryName)
    );

    public static IServiceCollection AddListAdminProductsEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IListAdminProductsEndpointQueryProvider, ListAdminProductsEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapListAdminProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsGroup.Names.ListAdminProducts);

        return app;
    }

    internal static async Task<Results<OkResult<ListAdminProductsResponse>, ValidationProblemResult>> HandleAsync(
        [AsParameters] ListAdminProductsRequest request,
        IListAdminProductsEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        request.PageNumber ??= DefaultPageNumber;
        request.PageSize ??= DefaultPageSize;

        // Validate sort parameters
        if (!SortingHelper.TryParseAndValidateSortString(request.OrderBy, AllowedSortColumns, out var sortColumns))
        {
            return ApiResults.ValidationProblem(nameof(request.OrderBy), ProductValidation.ErrorCodes.InvalidSortParameters);
        }

        var products = await queryProvider.GetProductsAsync(request, sortColumns, cancellationToken);
        var totalCount = await queryProvider.GetProductsCountAsync(request, cancellationToken);

        var response = new ListAdminProductsResponse(products, request.PageNumber.Value, request.PageSize.Value, totalCount);

        return ApiResults.Ok(response);
    }
}
