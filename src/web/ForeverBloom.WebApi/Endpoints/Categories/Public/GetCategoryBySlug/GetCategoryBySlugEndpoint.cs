using ForeverBloom.Application.Categories.Queries.GetCategoryBySlug;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Public.GetCategoryBySlug;

public static class GetCategoryBySlugEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoryBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetCategoryBySlugResponse>, PermanentRedirectResult, NotFoundResult, BadRequestResult>> HandleAsync(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = GetCategoryBySlugQuery.Create(slug);
        if (queryResult.IsFailure)
        {
            return ApiResults.NotFound(); // Invalid slug format - return 404 for public endpoint
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<GetCategoryBySlugResponse>, PermanentRedirectResult, NotFoundResult, BadRequestResult>>(
            onSuccess: category => ApiResults.Ok(GetCategoryBySlugResponse.FromResult(category)),
            onFailure: error => error switch
            {
                CategoryErrors.SlugChanged redirect => ApiResults.PermanentRedirect(
                    $"/api/v1/categories/{redirect.CurrentSlug}"),
                CategoryErrors.NotFoundBySlug => ApiResults.NotFound(),
                _ => ApiResults.BadRequest(error)
            });
    }
}
