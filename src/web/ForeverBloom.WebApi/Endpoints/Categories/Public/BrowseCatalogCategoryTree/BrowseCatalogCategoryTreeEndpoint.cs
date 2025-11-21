using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Public.BrowseCatalogCategoryTree;

internal static class BrowseCatalogCategoryTreeEndpoint
{
    internal static IEndpointRouteBuilder MapBrowseCatalogCategoryTreeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tree", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.BrowseCatalogCategoryTree);

        return app;
    }

    private static async Task<Results<OkResult<BrowseCatalogCategoryTreeResponse>, BadRequestResult>> HandleAsync(
        [AsParameters] BrowseCatalogCategoryTreeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = request.ToQuery();
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<BrowseCatalogCategoryTreeResponse>, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(BrowseCatalogCategoryTreeResponse.FromResult(payload)),
            onFailure: error => ApiResults.BadRequest(error));
    }
}
