using ForeverBloom.Api.Contracts.Catalog.Categories.Public.GetCategoryBySlug;
using ForeverBloom.Domain.Shared.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Public.GetCategoryBySlug;

public static class GetCategoryBySlugEndpoint
{
    public static IServiceCollection AddGetCategoryBySlugEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetCategoryBySlugEndpointQueryProvider, GetCategoryBySlugEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetCategoryBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(CategoryEndpointsGroup.Names.GetCategoryBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetCategoryBySlugResponse>, PermanentRedirectResult, NotFoundResult>> HandleAsync(
        string slug,
        IGetCategoryBySlugEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        // Validate slug format early and return 404 for invalid slugs (public endpoint behavior)
        if (string.IsNullOrWhiteSpace(slug) ||
            slug.Length > SlugValidation.Constants.MaxLength ||
            !Regex.IsMatch(slug, SlugValidation.Constants.Regex))
        {
            return ApiResults.NotFound();
        }

        // Look up the slug to find the target category and current slug
        var slugLookup = await queryProvider.GetSlugLookupAsync(slug, cancellationToken);

        if (slugLookup is null)
        {
            return ApiResults.NotFound();
        }

        // Try to get the category details (this will respect IsActive and DeletedAt filters)
        var categoryResponse = await queryProvider.GetCategoryByIdAsync(slugLookup.CategoryId, cancellationToken);

        if (categoryResponse is null)
        {
            // Category exists but is not accessible (inactive or archived)
            return ApiResults.NotFound();
        }

        // If the provided slug is not the current one, return 301 redirect
        if (!slugLookup.IsProvidedSlugCurrent)
        {
            return ApiResults.PermanentRedirect($"/api/v1/categories/{slugLookup.CurrentSlug}");
        }

        // Provided slug is current and category is accessible
        return ApiResults.Ok(categoryResponse);
    }
}
