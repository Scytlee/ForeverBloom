using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.BrowseCatalogProducts;

public static class BrowseCatalogProductsEndpoint
{
    internal static IEndpointRouteBuilder MapBrowseCatalogProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.BrowseCatalogProducts);

        return app;
    }

    private static async Task<Results<OkResult<BrowseCatalogProductsResponse>, ValidationProblemResult, BadRequestResult>> HandleAsync(
        [AsParameters] BrowseCatalogProductsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<BrowseCatalogProductsResponse>, ValidationProblemResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(BrowseCatalogProductsResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
