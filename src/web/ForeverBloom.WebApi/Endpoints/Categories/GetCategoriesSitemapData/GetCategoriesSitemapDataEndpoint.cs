using ForeverBloom.Application.Categories.Queries.GetCategoriesSitemapData;
using ForeverBloom.WebApi.Results;
using MediatR;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoriesSitemapData;

public static class GetCategoriesSitemapDataEndpoint
{
    internal static IEndpointRouteBuilder MapGetCategoriesSitemapDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap-data", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoriesSitemapData);

        return app;
    }

    private static async Task<OkResult<GetCategoriesSitemapDataResponse>> HandleAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCategoriesSitemapDataQuery();
        var result = await sender.Send(query, cancellationToken);

        return ApiResults.Ok(GetCategoriesSitemapDataResponse.FromResult(result.Value!));
    }
}
