using ForeverBloom.WebApi.Authentication;
using ForeverBloom.WebApi.Endpoints.Categories.CreateCategory;
using ForeverBloom.WebApi.Endpoints.Categories.GetCategoryById;
using ForeverBloom.WebApi.Endpoints.Categories.ReparentCategory;
using ForeverBloom.WebApi.Endpoints.Categories.ReslugCategory;
using ForeverBloom.WebApi.Endpoints.Categories.UpdateCategory;

namespace ForeverBloom.WebApi.Endpoints.Categories;

/// <summary>
/// Centralizes Category endpoint registration, routing, and metadata.
/// </summary>
public static class CategoryEndpointsModule
{
    /// <summary>
    /// Endpoint name constants for use in routing and OpenAPI documentation.
    /// </summary>
    public static class Names
    {
        public const string GetCategoryById = "GetCategoryById";
        public const string CreateCategory = "CreateCategory";
        public const string UpdateCategory = "UpdateCategory";
        public const string ReparentCategory = "ReparentCategory";
        public const string ReslugCategory = "ReslugCategory";
    }

    /// <summary>
    /// Tag constants for grouping endpoints in OpenAPI documentation.
    /// </summary>
    public static class Tags
    {
        public const string Categories = "Categories";
        public const string Admin = "Admin";
    }

    /// <summary>
    /// Registers Category endpoint dependencies (validators, presenters, HTTP-specific services).
    /// </summary>
    public static IServiceCollection AddCategoryEndpoints(this IServiceCollection services)
    {
        // No category-specific endpoint services to register yet
        return services;
    }

    /// <summary>
    /// Maps all Category endpoints to the provided route builder.
    /// </summary>
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var adminEndpointsGroup = app.MapGroup("/admin/categories")
            .WithTags(Tags.Categories, Tags.Admin)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        adminEndpointsGroup.MapGetCategoryByIdEndpoint();
        adminEndpointsGroup.MapCreateCategoryEndpoint();
        adminEndpointsGroup.MapUpdateCategoryEndpoint();
        adminEndpointsGroup.MapReparentCategoryEndpoint();
        adminEndpointsGroup.MapReslugCategoryEndpoint();

        return app;
    }
}
