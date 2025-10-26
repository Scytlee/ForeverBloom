using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryTree;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryTree;

public static class GetCategoryTreeEndpoint
{
    public static IServiceCollection AddGetCategoryTreeEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetCategoryTreeEndpointQueryProvider, GetCategoryTreeEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetCategoryTreeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tree", HandleAsync)
            .WithName(CategoryEndpointsGroup.Names.GetCategoryTree);

        return app;
    }

    internal static async Task<OkResult<GetCategoryTreeResponse>> HandleAsync(
        IGetCategoryTreeEndpointQueryProvider queryProvider,
        int? rootCategoryId,
        int? depth,
        CancellationToken cancellationToken)
    {
        var categoryTreeResponse = await queryProvider.GetCategoryTreeAsync(rootCategoryId, depth, cancellationToken);

        return ApiResults.Ok(categoryTreeResponse);
    }
}
