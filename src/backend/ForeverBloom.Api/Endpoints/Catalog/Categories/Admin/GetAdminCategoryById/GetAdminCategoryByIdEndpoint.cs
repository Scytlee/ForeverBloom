using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.GetAdminCategoryById;
using ForeverBloom.Api.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;

public static class GetAdminCategoryByIdEndpoint
{
    public static IServiceCollection AddGetAdminCategoryByIdEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetAdminCategoryByIdEndpointQueryProvider, GetAdminCategoryByIdEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetAdminCategoryByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{categoryId:int}", HandleAsync)
            .WithName(CategoryEndpointsGroup.Names.GetAdminCategoryById);

        return app;
    }

    internal static async Task<Results<OkResult<GetAdminCategoryByIdResponse>, NotFoundResult>> HandleAsync(
        int categoryId,
        bool? includeArchived,
        IGetAdminCategoryByIdEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        var categoryResponse = await queryProvider.GetCategoryByIdAsync(categoryId, includeArchived ?? false, cancellationToken);

        if (categoryResponse is null)
        {
            return ApiResults.NotFound();
        }

        return ApiResults.Ok(categoryResponse);
    }
}
