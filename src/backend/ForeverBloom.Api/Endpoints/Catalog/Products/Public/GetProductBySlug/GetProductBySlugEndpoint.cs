using ForeverBloom.Api.Contracts.Catalog.Products.Public.GetProductBySlug;
using ForeverBloom.Domain.Shared.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;
using ForeverBloom.Api.Results;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Public.GetProductBySlug;

public static class GetProductBySlugEndpoint
{
    public static IServiceCollection AddGetProductBySlugEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IGetProductBySlugEndpointQueryProvider, GetProductBySlugEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapGetProductBySlugEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{slug}", HandleAsync)
            .WithName(ProductEndpointsGroup.Names.GetProductBySlug);

        return app;
    }

    internal static async Task<Results<OkResult<GetProductBySlugResponse>, PermanentRedirectResult, NotFoundResult>> HandleAsync(
        string slug,
        IGetProductBySlugEndpointQueryProvider queryProvider,
        CancellationToken cancellationToken)
    {
        // Validate slug format early and return 404 for invalid slugs (public endpoint behavior)
        if (string.IsNullOrWhiteSpace(slug) ||
            slug.Length > SlugValidation.Constants.MaxLength ||
            !Regex.IsMatch(slug, SlugValidation.Constants.Regex))
        {
            return ApiResults.NotFound();
        }

        // Look up the slug to find the target product and current slug
        var slugLookup = await queryProvider.GetSlugLookupAsync(slug, cancellationToken);

        if (slugLookup is null)
        {
            return ApiResults.NotFound();
        }

        // Try to get the product details (this will respect IsActive and DeletedAt filters)
        var productResponse = await queryProvider.GetProductByIdAsync(slugLookup.ProductId, cancellationToken);

        if (productResponse is null)
        {
            // Product exists but is not accessible (inactive or archived)
            return ApiResults.NotFound();
        }

        // If the provided slug is not the current one, return 301 redirect
        if (!slugLookup.IsProvidedSlugCurrent)
        {
            return ApiResults.PermanentRedirect($"/api/v1/products/{slugLookup.CurrentSlug}");
        }

        // Provided slug is current and product is accessible
        return ApiResults.Ok(productResponse);
    }
}
