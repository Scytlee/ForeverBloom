using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.Application.Products.Queries.GetProductBySlug;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductBySlug;

public static class GetProductBySlugEndpoint
{
    public static IEndpointRouteBuilder MapGetProductBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.GetProductBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetProductBySlugResponse>, PermanentRedirectResult, NotFoundResult>> HandleAsync(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetProductBySlugQuery(slug),
            cancellationToken);

        return result.Match<Results<OkResult<GetProductBySlugResponse>, PermanentRedirectResult, NotFoundResult>>(
            onSuccess: product => ApiResults.Ok(GetProductBySlugResponse.FromResult(product)),
            onFailure: error => error switch
            {
                ProductErrors.SlugChanged redirect => ApiResults.PermanentRedirect(
                    $"/api/v1/products/{redirect.CurrentSlug}"),
                ProductErrors.NotFound => ApiResults.NotFound(),
                ValidationError => ApiResults.NotFound(), // Invalid slug format - return 404 for public endpoint
                _ => throw new InvalidOperationException(
                    $"Unexpected error type: {error.GetType().Name}")
            });
    }
}
