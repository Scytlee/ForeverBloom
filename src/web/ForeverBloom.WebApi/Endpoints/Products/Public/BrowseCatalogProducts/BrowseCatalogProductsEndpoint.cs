using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Public.BrowseCatalogProducts;

public static class BrowseCatalogProductsEndpoint
{
    internal static IEndpointRouteBuilder MapBrowseCatalogProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.BrowseCatalogProducts);

        return app;
    }

    private static async Task<Results<OkResult<BrowseCatalogProductsResponse>, BadRequestResult>> HandleAsync(
        [AsParameters] BrowseCatalogProductsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = request.ToQuery();
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<BrowseCatalogProductsResponse>, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(BrowseCatalogProductsResponse.FromResult(payload)),
            onFailure: error => ApiResults.BadRequest(error));
    }
}
