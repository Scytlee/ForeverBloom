using ForeverBloom.Api.Contracts.Catalog.Products.Admin.DeleteProduct;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.DeleteProduct;

public static class DeleteProductEndpoint
{
    public static IServiceCollection AddDeleteProductEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IDeleteProductEndpointQueryProvider, DeleteProductEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapDeleteProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{productId:int}", HandleAsync)
            .ValidateRequest<DeleteProductRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.DeleteProduct);

        return app;
    }

    internal static async Task<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult>> HandleAsync(
        int productId,
        [AsParameters] DeleteProductRequest request,
        IDeleteProductEndpointQueryProvider queryProvider,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            return ApiResults.ValidationProblem(nameof(productId), ProductValidation.ErrorCodes.IdInvalid);
        }

        if (!await queryProvider.ProductExistsAsync(productId, cancellationToken))
        {
            return ApiResults.NotFound();
        }

        if (!await queryProvider.ProductIsArchivedAsync(productId, cancellationToken))
        {
            return ApiResults.ValidationProblem(nameof(productId), ProductValidation.ErrorCodes.ProductNotArchived);
        }

        try
        {
            productRepository.DeleteProduct(productId, request.RowVersion!.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var productStillExists = await queryProvider.ProductExistsAsync(productId, cancellationToken);
            return productStillExists switch
            {
                // Product was modified concurrently by another request
                true => ApiResults.Conflict(ProductValidation.ErrorCodes.DeleteConcurrencyConflict),
                // Product was deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        logger.LogInformation("Product with ID {Id} has been permanently deleted", productId);
        return ApiResults.NoContent();
    }
}
