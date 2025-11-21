using ForeverBloom.Application.Products.Queries.GetProductBySlug;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Public.GetProductBySlug;

public static class GetProductBySlugEndpoint
{
    public static IEndpointRouteBuilder MapGetProductBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.GetProductBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetProductBySlugResponse>, PermanentRedirectResult, NotFoundResult, BadRequestResult>> HandleAsync(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = GetProductBySlugQuery.Create(slug);
        if (queryResult.IsFailure)
        {
            // Invalid slug format - return 404 for public endpoint
            return ApiResults.NotFound();
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<GetProductBySlugResponse>, PermanentRedirectResult, NotFoundResult, BadRequestResult>>(
            onSuccess: product => ApiResults.Ok(GetProductBySlugResponse.FromResult(product)),
            onFailure: error => error switch
            {
                ProductErrors.SlugChanged redirect => ApiResults.PermanentRedirect(
                    $"/api/v1/products/{redirect.CurrentSlug}"),
                ProductErrors.NotFoundBySlug => ApiResults.NotFound(),
                _ => ApiResults.BadRequest(error)
            });
    }
}
