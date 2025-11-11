using ForeverBloom.Application.Products.Queries.GetProductsSitemapData;
using ForeverBloom.WebApi.Results;
using MediatR;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductsSitemapData;

public static class GetProductsSitemapDataEndpoint
{
    internal static IEndpointRouteBuilder MapGetProductsSitemapDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap-data", HandleAsync)
            .WithName(ProductEndpointsModule.Names.GetProductsSitemapData);

        return app;
    }

    private static async Task<OkResult<GetProductsSitemapDataResponse>> HandleAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetProductsSitemapDataQuery();
        var result = await sender.Send(query, cancellationToken);

        return ApiResults.Ok(GetProductsSitemapDataResponse.FromResult(result.Value!));
    }
}
