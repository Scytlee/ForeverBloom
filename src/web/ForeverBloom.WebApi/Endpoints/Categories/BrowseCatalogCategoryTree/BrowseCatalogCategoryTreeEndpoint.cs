using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.BrowseCatalogCategoryTree;

internal static class BrowseCatalogCategoryTreeEndpoint
{
    internal static IEndpointRouteBuilder MapBrowseCatalogCategoryTreeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tree", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.BrowseCatalogCategoryTree);

        return app;
    }

    private static async Task<Results<OkResult<BrowseCatalogCategoryTreeResponse>, ValidationProblemResult, BadRequestResult>> HandleAsync(
        [AsParameters] BrowseCatalogCategoryTreeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<BrowseCatalogCategoryTreeResponse>, ValidationProblemResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(BrowseCatalogCategoryTreeResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ValidationError validationError => ApiResults.ValidationProblem(validationError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
