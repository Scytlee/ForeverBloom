using ForeverBloom.Api.Contracts.Catalog.Products.Admin.UpdateProduct;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.UpdateProduct;

public static class UpdateProductEndpoint
{
    public static IServiceCollection AddUpdateProductEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IUpdateProductEndpointQueryProvider, UpdateProductEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapUpdateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{productId:int}", HandleAsync)
            .ValidateRequest<UpdateProductRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.UpdateProduct);

        return app;
    }

    internal static async
        Task<Results<OkResult<UpdateProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult>>
        HandleAsync(
            int productId,
            UpdateProductRequest request,
            IUpdateProductEndpointQueryProvider queryProvider,
            IProductRepository productRepository,
            ISlugRegistryRepository slugRegistryRepository,
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

        // Validate slug change if provided
        if (request.Slug.IsSet && request.Slug.Value != product.CurrentSlug)
        {
            if (!await queryProvider.IsSlugAvailableAsync(request.Slug.Value, productId, cancellationToken))
            {
                return ApiResults.ValidationProblem(nameof(request.Slug), ProductValidation.ErrorCodes.SlugIsNotAvailable);
            }
        }

        // Validate category change if provided and fetch new category info
        CategoryInfo? newCategoryInfo = null;
        if (request.CategoryId.IsSet && request.CategoryId.Value != product.CategoryId)
        {
            newCategoryInfo = await queryProvider.GetCategoryInfoAsync(request.CategoryId.Value, cancellationToken);
            if (newCategoryInfo is null)
            {
                return ApiResults.ValidationProblem(nameof(request.CategoryId), ProductValidation.ErrorCodes.CategoryNotFound);
            }
        }

        try
        {
            // Apply only the fields that were provided in the PATCH request
            if (request.Name.IsSet)
                product.Name = request.Name.Value;

            if (request.SeoTitle.IsSet)
                product.SeoTitle = request.SeoTitle.Value;

            if (request.FullDescription.IsSet)
                product.FullDescription = request.FullDescription.Value;

            if (request.MetaDescription.IsSet)
                product.MetaDescription = request.MetaDescription.Value;

            if (request.Slug.IsSet)
            {
                product.CurrentSlug = request.Slug.Value;
                await slugRegistryRepository.UpdateEntitySlugAsync(EntityType.Product, productId, request.Slug.Value, cancellationToken);
            }

            if (request.Price.IsSet)
                product.Price = request.Price.Value;

            if (request.CategoryId.IsSet)
                product.CategoryId = request.CategoryId.Value;

            if (request.DisplayOrder.IsSet)
                product.DisplayOrder = request.DisplayOrder.Value;

            if (request.IsFeatured.IsSet)
                product.IsFeatured = request.IsFeatured.Value;

            if (request.PublishStatus.IsSet)
                product.PublishStatus = request.PublishStatus.Value;

            if (request.Availability.IsSet)
                product.Availability = request.Availability.Value;

            productRepository.UpdateProduct(product);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var productExists = await queryProvider.ProductExistsAsync(productId, cancellationToken);
            return productExists switch
            {
                // Modified concurrently by another request
                true => ApiResults.Conflict(ProductValidation.ErrorCodes.UpdateConcurrencyConflict),
                // Deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        var response = new UpdateProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            SeoTitle = product.SeoTitle,
            FullDescription = product.FullDescription,
            MetaDescription = product.MetaDescription,
            Slug = product.CurrentSlug,
            Price = product.Price,
            DisplayOrder = product.DisplayOrder,
            IsFeatured = product.IsFeatured,
            PublishStatus = product.PublishStatus,
            Availability = product.Availability,
            CategoryId = product.CategoryId,
            CategoryName = newCategoryInfo?.Name ?? product.Category.Name,
            CategorySlug = newCategoryInfo?.CurrentSlug ?? product.Category.CurrentSlug,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new AdminProductImageItem
                {
                    ImagePath = i.ImagePath,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder,
                    AltText = i.AltText
                })
                .ToList(),
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            DeletedAt = product.DeletedAt,
            RowVersion = product.RowVersion
        };

        logger.LogInformation("Product with ID {Id} has been updated with changes: {@Changed}",
            response.Id,
            new
            {
                Name = request.Name.IsSet,
                SeoTitle = request.SeoTitle.IsSet,
                FullDescription = request.FullDescription.IsSet,
                MetaDescription = request.MetaDescription.IsSet,
                Slug = request.Slug.IsSet,
                Price = request.Price.IsSet,
                DisplayOrder = request.DisplayOrder.IsSet,
                IsFeatured = request.IsFeatured.IsSet,
                PublishStatus = request.PublishStatus.IsSet,
                Availability = request.Availability.IsSet,
                CategoryId = request.CategoryId.IsSet
            });

        return ApiResults.Ok(response);
    }
}
