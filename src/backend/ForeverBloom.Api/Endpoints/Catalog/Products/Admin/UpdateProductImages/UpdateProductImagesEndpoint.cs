using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProductImages;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProductImages;

public static class UpdateProductImagesEndpoint
{
    public static IServiceCollection AddUpdateProductImagesEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IUpdateProductImagesEndpointQueryProvider, UpdateProductImagesEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapUpdateProductImagesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/{productId:int}/images", HandleAsync)
            .ValidateRequest<UpdateProductImagesRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.UpdateProductImages);

        return app;
    }

    internal static async
        Task<Results<OkResult<UpdateProductImagesResponse>, NotFoundResult, ConflictResult>>
        HandleAsync(
            int productId,
            UpdateProductImagesRequest request,
            IUpdateProductImagesEndpointQueryProvider queryProvider,
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
            return ApiResults.Conflict(ProductValidation.ErrorCodes.UpdateConcurrencyConflict);
        }

        try
        {
            // Create new image entities
            var newImages = request.Images.Select(imageItem => new ProductImage
            {
                ImagePath = imageItem.ImagePath,
                IsPrimary = imageItem.IsPrimary,
                DisplayOrder = imageItem.DisplayOrder,
                AltText = imageItem.AltText
            });

            // Replace images using repository method
            productRepository.ReplaceProductImages(product, newImages);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResults.Conflict(ProductValidation.ErrorCodes.UpdateConcurrencyConflict);
        }

        var response = new UpdateProductImagesResponse
        {
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageItem
                {
                    ImagePath = i.ImagePath,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder,
                    AltText = i.AltText
                })
                .ToList(),
            RowVersion = product.RowVersion
        };

        logger.LogInformation("Product images for product with ID {Id} have been updated with {ImageCount} images",
            productId, response.Images.Count);
        return ApiResults.Ok(response);
    }
}
