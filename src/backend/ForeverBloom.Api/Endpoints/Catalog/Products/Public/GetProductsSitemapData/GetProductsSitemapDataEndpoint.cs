using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductsSitemapData;

public static class GetProductsSitemapDataEndpoint
{
    public static IServiceCollection AddGetProductsSitemapDataEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetProductsSitemapDataEndpointQueryProvider, GetProductsSitemapDataEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetProductsSitemapDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap-data", HandleAsync)
            .WithName(ProductEndpointsGroup.Names.GetProductsSitemapData);

        return app;
    }

    internal static async Task<OkResult<GetProductsSitemapDataResponse>> HandleAsync(
        IGetProductsSitemapDataEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        var items = await queryProvider.GetProductsSitemapDataAsync(cancellationToken);
        var response = new GetProductsSitemapDataResponse { Items = items };

        return ApiResults.Ok(response);
    }
}
