using ForeverBloom.Api.Contracts.Catalog.Products.Admin.GetAdminProductById;
using ForeverBloom.Api.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.GetAdminProductById;

public static class GetAdminProductByIdEndpoint
{
    public static IServiceCollection AddGetAdminProductByIdEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetAdminProductByIdEndpointQueryProvider, GetAdminProductByIdEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetAdminProductByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{productId:int}", HandleAsync)
            .WithName(ProductEndpointsGroup.Names.GetAdminProductById);

        return app;
    }

    internal static async Task<Results<OkResult<GetAdminProductByIdResponse>, NotFoundResult>> HandleAsync(
        int productId,
        bool? includeArchived,
        IGetAdminProductByIdEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        var productResponse = await queryProvider.GetProductByIdAsync(productId, includeArchived ?? false, cancellationToken);

        if (productResponse is null)
        {
            return ApiResults.NotFound();
        }

        return ApiResults.Ok(productResponse);
    }
}
