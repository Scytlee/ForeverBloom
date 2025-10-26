using ForeverBloom.Api.Contracts.Catalog.Products.Admin.ArchiveProduct;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.ArchiveProduct;

public static class ArchiveProductEndpoint
{
    public static IServiceCollection AddArchiveProductEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IArchiveProductEndpointQueryProvider, ArchiveProductEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapArchiveProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:int}/archive", HandleAsync)
            .ValidateRequest<ArchiveProductRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.ArchiveProduct);

        return app;
    }

    internal static async Task<Results<OkResult<ArchiveProductResponse>, NotFoundResult, ConflictResult>> HandleAsync(
        int productId,
        ArchiveProductRequest request,
        IArchiveProductEndpointQueryProvider queryProvider,
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
            return ApiResults.Conflict(ProductValidation.ErrorCodes.ArchiveConcurrencyConflict);
        }

        try
        {
            productRepository.ArchiveProduct(product);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var productStillExists = await queryProvider.ProductExistsAsync(productId, cancellationToken);
            return productStillExists switch
            {
                // Product was modified concurrently by another request
                true => ApiResults.Conflict(ProductValidation.ErrorCodes.ArchiveConcurrencyConflict),
                // Product was deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        var response = new ArchiveProductResponse
        {
            DeletedAt = product.DeletedAt!.Value,
            RowVersion = product.RowVersion
        };

        logger.LogInformation("Product with ID {Id} has been archived at {DeletedAt:o}", product.Id, response.DeletedAt);
        return ApiResults.Ok(response);
    }
}
