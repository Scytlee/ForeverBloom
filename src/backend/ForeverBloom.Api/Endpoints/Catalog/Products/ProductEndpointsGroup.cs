using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ArchiveProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.DeleteProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.GetAdminProductById;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ListAdminProducts;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.RestoreProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductsSitemapData;
using ForeverBloom.Api.Endpoints.Catalog.Products.Public.ListProducts;

namespace ForeverBloom.Api.Endpoints.Catalog.Products;

public static class ProductEndpointsGroup
{
    public static class Names
    {
        // Public
        public const string ListProducts = "ListProducts";
        public const string GetProductBySlug = "GetProductBySlug";
        public const string GetProductsSitemapData = "GetProductsSitemapData";

        // Admin
        public const string CreateProduct = "CreateProduct";
        public const string ListAdminProducts = "ListAdminProducts";
        public const string GetAdminProductById = "GetAdminProductById";
        public const string UpdateProduct = "UpdateProduct";
        public const string UpdateProductImages = "UpdateProductImages";
        public const string ArchiveProduct = "ArchiveProduct";
        public const string RestoreProduct = "RestoreProduct";
        public const string DeleteProduct = "DeleteProduct";
    }

    public static class Tags
    {
        public const string Products = "Products";
        public const string Public = "Public";
        public const string Admin = "Admin";

    }

    public static IServiceCollection AddProductEndpoints(this IServiceCollection services)
    {
        // Public endpoints
        services.AddListProductsEndpoint();
        services.AddGetProductBySlugEndpoint();
        services.AddGetProductsSitemapDataEndpoint();

        // Admin endpoints
        services.AddCreateProductEndpoint();
        services.AddListAdminProductsEndpoint();
        services.AddGetAdminProductByIdEndpoint();
        services.AddUpdateProductEndpoint();
        services.AddUpdateProductImagesEndpoint();
        services.AddArchiveProductEndpoint();
        services.AddRestoreProductEndpoint();
        services.AddDeleteProductEndpoint();

        return services;
    }

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Public endpoints
        var publicProductsGroup = app.MapGroup("/products")
            .WithTags(Tags.Products, Tags.Public)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        publicProductsGroup.MapListProductsEndpoint();
        publicProductsGroup.MapGetProductBySlugEndpoint();
        publicProductsGroup.MapGetProductsSitemapDataEndpoint();

        // Admin endpoints
        var adminProductsGroup = app.MapGroup("/admin/products")
            .WithTags(Tags.Products, Tags.Admin)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        adminProductsGroup.MapCreateProductEndpoint();
        adminProductsGroup.MapListAdminProductsEndpoint();
        adminProductsGroup.MapGetAdminProductByIdEndpoint();
        adminProductsGroup.MapUpdateProductEndpoint();
        adminProductsGroup.MapUpdateProductImagesEndpoint();
        adminProductsGroup.MapArchiveProductEndpoint();
        adminProductsGroup.MapRestoreProductEndpoint();
        adminProductsGroup.MapDeleteProductEndpoint();

        return app;
    }
}
