using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoriesSitemapData;

public static class GetCategoriesSitemapDataEndpoint
{
    public static IServiceCollection AddGetCategoriesSitemapDataEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetCategoriesSitemapDataEndpointQueryProvider, GetCategoriesSitemapDataEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetCategoriesSitemapDataEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap-data", HandleAsync)
            .WithName(CategoryEndpointsGroup.Names.GetCategoriesSitemapData);

        return app;
    }

    internal static async Task<OkResult<GetCategoriesSitemapDataResponse>> HandleAsync(
        IGetCategoriesSitemapDataEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        var items = await queryProvider.GetCategoriesSitemapDataAsync(cancellationToken);
        var response = new GetCategoriesSitemapDataResponse { Items = items };

        return ApiResults.Ok(response);
    }
}
