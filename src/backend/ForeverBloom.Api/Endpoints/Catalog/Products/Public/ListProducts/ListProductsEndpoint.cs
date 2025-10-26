using ForeverBloom.Api.Contracts.Catalog.Products.Public.ListProducts;
using ForeverBloom.Api.Helpers;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.ListProducts;

public static class ListProductsEndpoint
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> AllowedSortColumns = SortingHelper.CreateAllowedSortColumns(
        nameof(PublicProductListItem.Name),
        nameof(PublicProductListItem.Price),
        nameof(PublicProductListItem.CategoryName)
    );

    public static IServiceCollection AddListProductsEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IListProductsEndpointQueryProvider, ListProductsEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapListProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsGroup.Names.ListProducts);

        return app;
    }

    internal static async Task<Results<OkResult<ListProductsResponse>, ValidationProblemResult>> HandleAsync(
        [AsParameters] ListProductsRequest request,
        IListProductsEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        request.PageNumber ??= DefaultPageNumber;
        request.PageSize ??= DefaultPageSize;

        // Ensure page size doesn't exceed maximum
        if (request.PageSize > MaxPageSize)
        {
            request.PageSize = MaxPageSize;
        }

        // Validate sort parameters
        if (!SortingHelper.TryParseAndValidateSortString(request.OrderBy, AllowedSortColumns, out var sortColumns))
        {
            return ApiResults.ValidationProblem(nameof(request.OrderBy), ProductValidation.ErrorCodes.InvalidSortParameters);
        }

        var products = await queryProvider.GetProductsAsync(request, sortColumns, cancellationToken);
        var totalCount = await queryProvider.GetProductsCountAsync(request, cancellationToken);

        var response = new ListProductsResponse(products, request.PageNumber.Value, request.PageSize.Value, totalCount);

        return ApiResults.Ok(response);
    }
}
