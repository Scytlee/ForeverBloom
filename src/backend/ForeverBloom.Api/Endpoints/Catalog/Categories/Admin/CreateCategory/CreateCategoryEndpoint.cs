using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.CreateCategory;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.CreateCategory;

public static class CreateCategoryEndpoint
{
    public static IServiceCollection AddCreateCategoryEndpoint(this IServiceCollection services)
    {
        services.AddScoped<ICreateCategoryEndpointQueryProvider, CreateCategoryEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .ValidateRequest<CreateCategoryRequest>()
            .UseUnitOfWork()
            .WithName(CategoryEndpointsGroup.Names.CreateCategory);

        return app;
    }

    internal static async Task<Results<CreatedResult<CreateCategoryResponse>, ValidationProblemResult>>
        HandleAsync(
            CreateCategoryRequest request,
            IUnitOfWork unitOfWork,
            ICategoryRepository categoryRepository,
            ISlugRegistryRepository slugRegistryRepository,
            ICreateCategoryEndpointQueryProvider queryProvider,
            ILogger logger,
            CancellationToken cancellationToken)
    {
        // Check if slug is available
        if (!await queryProvider.IsSlugAvailableAsync(request.Slug, cancellationToken))
        {
            return ApiResults.ValidationProblem(nameof(request.Slug),
                CategoryValidation.ErrorCodes.SlugIsNotAvailable);
        }

        // Check if name already exists within same parent
        if (await queryProvider.CategoryNameExistsWithinParentAsync(request.Name, request.ParentCategoryId, cancellationToken))
        {
            return ApiResults.ValidationProblem(nameof(request.Name),
                CategoryValidation.ErrorCodes.NameNotUniqueWithinParent);
        }

        // Check if parent category exists and get its path (if specified)
        string? parentPath = null;
        if (request.ParentCategoryId.HasValue)
        {
            parentPath = await queryProvider.GetCategoryPathByIdAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parentPath is null)
            {
                return ApiResults.ValidationProblem(nameof(request.ParentCategoryId), CategoryValidation.ErrorCodes.ParentCategoryNotFound);
            }

            // Check hierarchy depth limit
            var parentDepth = await queryProvider.GetParentHierarchyDepthAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parentDepth >= CategoryValidation.Constants.MaxHierarchyDepth)
            {
                return ApiResults.ValidationProblem(nameof(request.ParentCategoryId),
                    CategoryValidation.ErrorCodes.MaxHierarchyDepthExceeded);
            }
        }

        // Build the path for the new category
        var categoryPath = parentPath is not null
            ? new LTree(parentPath + "." + request.Slug)
            : new LTree(request.Slug);

        var newCategory = new Category
        {
            Name = request.Name,
            Description = request.Description,
            CurrentSlug = request.Slug,
            ImagePath = request.ImagePath,
            ParentCategoryId = request.ParentCategoryId,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            Path = categoryPath
        };

        categoryRepository.InsertCategory(newCategory);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var slugEntry = new SlugRegistryEntry
        {
            Slug = request.Slug,
            EntityType = EntityType.Category,
            EntityId = newCategory.Id,
            IsActive = true
        };

        slugRegistryRepository.InsertSlugRegistryEntry(slugEntry);

        logger.LogInformation("Category with ID {Id} has been created with slug '{Slug}' and parent category ID {ParentCategoryId}",
            newCategory.Id, newCategory.CurrentSlug, newCategory.ParentCategoryId);

        var response = new CreateCategoryResponse
        {
            Id = newCategory.Id,
            Name = newCategory.Name,
            Description = newCategory.Description,
            Slug = newCategory.CurrentSlug,
            ImagePath = newCategory.ImagePath,
            ParentCategoryId = newCategory.ParentCategoryId,
            DisplayOrder = newCategory.DisplayOrder,
            IsActive = newCategory.IsActive,
            CreatedAt = newCategory.CreatedAt,
            UpdatedAt = newCategory.UpdatedAt,
            DeletedAt = newCategory.DeletedAt,
            RowVersion = newCategory.RowVersion
        };

        return ApiResults.Created($"/api/v1/admin/categories/{newCategory.Id}", response);
    }
}
