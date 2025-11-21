using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ListProducts;

public static class ListProductsEndpoint
{
    internal static IEndpointRouteBuilder MapListProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ListProducts);

        return app;
    }

    private static async Task<Results<OkResult<ListProductsResponse>, BadRequestResult>> HandleAsync(
        [AsParameters] ListProductsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = request.ToQuery();
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ListProductsResponse>, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ListProductsResponse.FromResult(payload)),
            onFailure: error => ApiResults.BadRequest(error));
    }
}
