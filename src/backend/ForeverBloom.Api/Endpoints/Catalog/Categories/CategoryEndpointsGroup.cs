using ForeverBloom.Api.Authentication;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.DeleteCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.GetAdminCategoryById;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ListAdminCategories;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.RestoreCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoriesSitemapData;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryTree;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories;

public static class CategoryEndpointsGroup
{
    public static class Names
    {
        // Public
        public const string GetCategoryTree = "GetCategoryTree";
        public const string GetCategoryBySlug = "GetCategoryBySlug";
        public const string GetCategoriesSitemapData = "GetCategoriesSitemapData";

        // Admin
        public const string CreateCategory = "CreateCategory";
        public const string ListAdminCategories = "ListAdminCategories";
        public const string GetAdminCategoryById = "GetAdminCategoryById";
        public const string UpdateCategory = "UpdateCategory";
        public const string ArchiveCategory = "ArchiveCategory";
        public const string RestoreCategory = "RestoreCategory";
        public const string DeleteCategory = "DeleteCategory";
    }

    public static class Tags
    {
        public const string Categories = "Categories";
        public const string Public = "Public";
        public const string Admin = "Admin";
    }

    public static IServiceCollection AddCategoryEndpoints(this IServiceCollection services)
    {
        // Public endpoints
        services.AddGetCategoryTreeEndpoint();
        services.AddGetCategoryBySlugEndpoint();
        services.AddGetCategoriesSitemapDataEndpoint();

        // Admin endpoints
        services.AddCreateCategoryEndpoint();
        services.AddListAdminCategoriesEndpoint();
        services.AddGetAdminCategoryByIdEndpoint();
        services.AddUpdateCategoryEndpoint();
        services.AddArchiveCategoryEndpoint();
        services.AddRestoreCategoryEndpoint();
        services.AddDeleteCategoryEndpoint();

        return services;
    }

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Public endpoints
        var publicCategoriesGroup = app.MapGroup("/categories")
            .WithTags(Tags.Categories, Tags.Public)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.FrontendAccessPolicyName);
        publicCategoriesGroup.MapGetCategoryTreeEndpoint();
        publicCategoriesGroup.MapGetCategoryBySlugEndpoint();
        publicCategoriesGroup.MapGetCategoriesSitemapDataEndpoint();

        // Admin endpoints
        var adminCategoriesGroup = app.MapGroup("/admin/categories")
            .WithTags(Tags.Categories, Tags.Admin)
            .RequireAuthorization(ApiKeyAuthenticationDefaults.AdminAccessPolicyName);
        adminCategoriesGroup.MapCreateCategoryEndpoint();
        adminCategoriesGroup.MapListAdminCategoriesEndpoint();
        adminCategoriesGroup.MapGetAdminCategoryByIdEndpoint();
        adminCategoriesGroup.MapUpdateCategoryEndpoint();
        adminCategoriesGroup.MapArchiveCategoryEndpoint();
        adminCategoriesGroup.MapRestoreCategoryEndpoint();
        adminCategoriesGroup.MapDeleteCategoryEndpoint();

        return app;
    }
}
