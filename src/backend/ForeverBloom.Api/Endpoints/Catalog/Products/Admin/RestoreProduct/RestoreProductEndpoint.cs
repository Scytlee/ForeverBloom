using ForeverBloom.Api.Contracts.Catalog.Products.Admin.RestoreProduct;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.RestoreProduct;

public static class RestoreProductEndpoint
{
    public static IServiceCollection AddRestoreProductEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IRestoreProductEndpointQueryProvider, RestoreProductEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapRestoreProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:int}/restore", HandleAsync)
            .ValidateRequest<RestoreProductRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.RestoreProduct);

        return app;
    }

    internal static async Task<Results<OkResult<RestoreProductResponse>, NotFoundResult, ConflictResult>> HandleAsync(
        int productId,
        RestoreProductRequest request,
        IRestoreProductEndpointQueryProvider queryProvider,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var product = await queryProvider.GetProductAsync(productId, cancellationToken);

        if (product is null)
        {
            return ApiResults.NotFound();
        }

        // Optimistic concurrency check
        if (product.RowVersion != request.RowVersion)
        {
            return ApiResults.Conflict(ProductValidation.ErrorCodes.RestoreConcurrencyConflict);
        }

        try
        {
            productRepository.RestoreProduct(product);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var productStillExists = await queryProvider.ProductExistsAsync(productId, cancellationToken);
            return productStillExists switch
            {
                // Product was modified concurrently by another request
                true => ApiResults.Conflict(ProductValidation.ErrorCodes.RestoreConcurrencyConflict),
                // Product was deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        var response = new RestoreProductResponse
        {
            DeletedAt = null,
            RowVersion = product.RowVersion
        };

        logger.LogInformation("Product with ID {Id} has been restored", product.Id);
        return ApiResults.Ok(response);
    }
}
