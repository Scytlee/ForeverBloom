using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;

public static class CreateProductEndpoint
{
    public static IServiceCollection AddCreateProductEndpoint(this IServiceCollection services)
    {
        services.AddScoped<ICreateProductEndpointQueryProvider, CreateProductEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .ValidateRequest<CreateProductRequest>()
            .UseUnitOfWork()
            .WithName(ProductEndpointsGroup.Names.CreateProduct);

        return app;
    }

    internal static async Task<Results<CreatedResult<CreateProductResponse>, ValidationProblemResult>>
        HandleAsync(
            CreateProductRequest request,
            IUnitOfWork unitOfWork,
            IProductRepository productRepository,
            ISlugRegistryRepository slugRegistryRepository,
            ICreateProductEndpointQueryProvider queryProvider,
            ILogger logger,
            CancellationToken cancellationToken)
    {
        // Check if slug is available
        if (!await queryProvider.IsSlugAvailableAsync(request.Slug, cancellationToken))
        {
            return ApiResults.ValidationProblem(nameof(request.Slug),
                ProductValidation.ErrorCodes.SlugIsNotAvailable);
        }

        // Check if category exists
        if (!await queryProvider.CategoryExistsAsync(request.CategoryId, cancellationToken))
        {
            return ApiResults.ValidationProblem(nameof(request.CategoryId),
                ProductValidation.ErrorCodes.CategoryNotFound);
        }

        var newProduct = new Product
        {
            Name = request.Name,
            SeoTitle = request.SeoTitle,
            FullDescription = request.FullDescription,
            MetaDescription = request.MetaDescription,
            CurrentSlug = request.Slug,
            Price = request.Price,
            DisplayOrder = request.DisplayOrder,
            IsFeatured = request.IsFeatured,
            PublishStatus = request.PublishStatus,
            Availability = request.Availability,
            CategoryId = request.CategoryId
        };

        productRepository.InsertProduct(newProduct);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var slugEntry = new SlugRegistryEntry
        {
            Slug = request.Slug,
            EntityType = EntityType.Product,
            EntityId = newProduct.Id,
            IsActive = true
        };

        slugRegistryRepository.InsertSlugRegistryEntry(slugEntry);

        logger.LogInformation("Product with ID {Id} has been created with slug '{Slug}' in category ID {CategoryId}",
            newProduct.Id, newProduct.CurrentSlug, newProduct.CategoryId);

        var response = new CreateProductResponse
        {
            Id = newProduct.Id,
            Name = newProduct.Name,
            SeoTitle = newProduct.SeoTitle,
            FullDescription = newProduct.FullDescription,
            MetaDescription = newProduct.MetaDescription,
            Slug = newProduct.CurrentSlug,
            Price = newProduct.Price,
            DisplayOrder = newProduct.DisplayOrder,
            IsFeatured = newProduct.IsFeatured,
            PublishStatus = newProduct.PublishStatus,
            Availability = newProduct.Availability,
            CategoryId = newProduct.CategoryId,
            CreatedAt = newProduct.CreatedAt,
            UpdatedAt = newProduct.UpdatedAt,
            DeletedAt = newProduct.DeletedAt,
            RowVersion = newProduct.RowVersion
        };

        return ApiResults.Created($"/api/v1/admin/products/{newProduct.Id}", response);
    }
}
