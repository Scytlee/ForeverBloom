using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Categories;
using ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoryBySlug;

public static class GetCategoryBySlugEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoryBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetCategoryBySlugResponse>, PermanentRedirectResult, NotFoundResult>> HandleAsync(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCategoryBySlugQuery(slug);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<GetCategoryBySlugResponse>, PermanentRedirectResult, NotFoundResult>>(
            onSuccess: category => ApiResults.Ok(GetCategoryBySlugResponse.FromResult(category)),
            onFailure: error => error switch
            {
                CategoryErrors.SlugChanged redirect => ApiResults.PermanentRedirect(
                    $"/api/v1/categories/{redirect.CurrentSlug}"),
                CategoryErrors.NotFound => ApiResults.NotFound(),
                ValidationError => ApiResults.NotFound(), // Invalid slug format - return 404 for public endpoint
                _ => throw new InvalidOperationException(
                    $"Unexpected error type: {error.GetType().Name}")
            });
    }
}
