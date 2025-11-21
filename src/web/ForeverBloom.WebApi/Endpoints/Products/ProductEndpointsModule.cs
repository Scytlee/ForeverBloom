using ForeverBloom.WebApi.Authentication;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ArchiveProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.CreateProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.DeleteProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.GetProductById;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ListProducts;
using ForeverBloom.WebApi.Endpoints.Products.Admin.ReslugProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.RestoreProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProduct;
using ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProductImages;
using ForeverBloom.WebApi.Endpoints.Products.Public.BrowseCatalogProducts;
using ForeverBloom.WebApi.Endpoints.Products.Public.GetProductBySlug;
using ForeverBloom.WebApi.Endpoints.Products.Public.GetProductsSitemapData;

namespace ForeverBloom.WebApi.Endpoints.Products;

/// <summary>
/// Centralizes Product endpoint registration, routing, and metadata (names, tags).
/// Provides a single place to manage product endpoint concerns at the Web API layer.
/// </summary>
public static class ProductEndpointsModule
{
    /// <summary>
    /// Endpoint name constants for use in routing and OpenAPI documentation.
    /// </summary>
    public static class Names
    {
        // Public
        public const string BrowseCatalogProducts = "BrowseCatalogProducts";
        public const string GetProductBySlug = "GetProductBySlug";
        public const string GetProductsSitemapData = "GetProductsSitemapData";

        // Admin
        public const string ListProducts = "ListProducts";
        public const string GetProductById = "GetProductById";
        public const string CreateProduct = "CreateProduct";
        public const string UpdateProductImages = "UpdateProductImages";
        public const string UpdateProduct = "UpdateProduct";
        public const string ReslugProduct = "ReslugProduct";
        public const string ArchiveProduct = "ArchiveProduct";
        public const string RestoreProduct = "RestoreProduct";
        public const string DeleteProduct = "DeleteProduct";
    }

    /// <summary>
    /// Tag constants for grouping endpoints in OpenAPI documentation.
    /// </summary>
    public static class Tags
    {
        public const string Products = "Products";
        public const string Public = "Public";
        public const string Admin = "Admin";
    }

    /// <summary>
    /// Registers Product endpoint dependencies (validators, presenters, HTTP-specific services).
    /// </summary>
    public static IServiceCollection AddProductEndpoints(this IServiceCollection services)
    {
        // No product-specific endpoint services to register yet
        return services;
    }

    /// <summary>
    /// Maps all Product endpoints to the provided route builder.
    /// Creates endpoint groups and applies shared routing, auth, and metadata.
    /// </summary>
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var publicEndpointsGroup = app.MapGroup("/products")
            .WithTags(Tags.Products, Tags.Public)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        publicEndpointsGroup.MapBrowseCatalogProductsEndpoint();
        publicEndpointsGroup.MapGetProductBySlugEndpoint();
        publicEndpointsGroup.MapGetProductsSitemapDataEndpoint();

        var adminEndpointsGroup = app.MapGroup("/admin/products")
            .WithTags(Tags.Products, Tags.Admin)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        adminEndpointsGroup.MapListProductsEndpoint();
        adminEndpointsGroup.MapGetProductByIdEndpoint();
        adminEndpointsGroup.MapCreateProductEndpoint();
        adminEndpointsGroup.MapUpdateProductImagesEndpoint();
        adminEndpointsGroup.MapUpdateProductEndpoint();
        adminEndpointsGroup.MapReslugProductEndpoint();
        adminEndpointsGroup.MapArchiveProductEndpoint();
        adminEndpointsGroup.MapRestoreProductEndpoint();
        adminEndpointsGroup.MapDeleteProductEndpoint();

        return app;
    }
}
