using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.UpdateCategory;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    public static IServiceCollection AddUpdateCategoryEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IUpdateCategoryEndpointQueryProvider, UpdateCategoryEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{categoryId:int}", HandleAsync)
            .ValidateRequest<UpdateCategoryRequest>()
            .UseUnitOfWork()
            .WithName(CategoryEndpointsGroup.Names.UpdateCategory);

        return app;
    }

    internal static async
        Task<Results<OkResult<UpdateCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult>>
        HandleAsync(
            int categoryId,
            UpdateCategoryRequest request,
            IUpdateCategoryEndpointQueryProvider queryProvider,
            ICategoryRepository categoryRepository,
            ISlugRegistryRepository slugRegistryRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
    {
        var category = await queryProvider.GetCategoryAsync(categoryId, cancellationToken);

        if (category is null)
        {
            return ApiResults.NotFound();
        }

        // Optimistic concurrency check
        if (category.RowVersion != request.RowVersion)
        {
            return ApiResults.Conflict(CategoryValidation.ErrorCodes.UpdateConcurrencyConflict);
        }

        bool? categoryHasChildren = null;

        // Validate slug changes if provided
        if (request.Slug.IsSet && request.Slug.Value != category.CurrentSlug)
        {
            // Only allow slug changes for childless categories
            categoryHasChildren ??= await queryProvider.CategoryHasChildrenAsync(categoryId, cancellationToken);
            if (categoryHasChildren.Value)
            {
                return ApiResults.ValidationProblem(nameof(request.Slug), CategoryValidation.ErrorCodes.HierarchyChangeNotAllowed);
            }

            // Validate slug availability
            if (!await queryProvider.IsSlugAvailableAsync(request.Slug.Value, categoryId, cancellationToken))
            {
                return ApiResults.ValidationProblem(nameof(request.Slug), CategoryValidation.ErrorCodes.SlugIsNotAvailable);
            }
        }

        // Validate parent change if provided and get new parent's Path if needed
        string? newParentPath = null;
        if (request.ParentCategoryId.IsSet && request.ParentCategoryId.Value != category.ParentCategoryId)
        {
            // Only allow parent changes for childless categories
            categoryHasChildren ??= await queryProvider.CategoryHasChildrenAsync(categoryId, cancellationToken);
            if (categoryHasChildren.Value)
            {
                return ApiResults.ValidationProblem(nameof(request.ParentCategoryId), CategoryValidation.ErrorCodes.HierarchyChangeNotAllowed);
            }

            // Validate that the new parent category exists (if not null) and get its Path
            if (request.ParentCategoryId.Value.HasValue)
            {
                newParentPath = await queryProvider.GetCategoryPathAsync(request.ParentCategoryId.Value.Value, cancellationToken);
                if (newParentPath is null)
                {
                    return ApiResults.ValidationProblem(nameof(request.ParentCategoryId), CategoryValidation.ErrorCodes.ParentCategoryNotFound);
                }
            }

        }

        // TODO: FUTURE ENHANCEMENT - Add allowHierarchyChanges parameter
        // ARCHITECTURAL DECISION: Slug and parent category changes for entities that have any descendants
        // will trigger complex implicit side effects, which is not allowed according to API design
        // This includes the following:
        // 1. Cascading path updates to all descendants
        // 2. Complex circular reference detection using ltree operators
        // 3. Batch updates for performance with large hierarchies

        // Check if name already exists at the same level (siblings under same parent or at root level)
        if (request.Name.IsSet)
        {
            var effectiveParentId = request.ParentCategoryId.IsSet ? request.ParentCategoryId.Value : category.ParentCategoryId;
            if (await queryProvider.CategoryNameExistsAtSameLevelAsync(request.Name.Value, effectiveParentId, categoryId, cancellationToken))
            {
                return ApiResults.ValidationProblem(nameof(request.Name),
                    CategoryValidation.ErrorCodes.NameNotUniqueWithinParent);
            }
        }

        try
        {
            // Placeholder for Path update
            List<string>? pathParts = null;

            if (request.Name.IsSet)
                category.Name = request.Name.Value;

            if (request.Description.IsSet)
                category.Description = request.Description.Value;

            if (request.Slug.IsSet)
            {
                category.CurrentSlug = request.Slug.Value;
                await slugRegistryRepository.UpdateEntitySlugAsync(EntityType.Category, categoryId, request.Slug.Value, cancellationToken);

                // Slug update requires Path update
                pathParts ??= category.Path.ToString().Split('.').ToList();
                pathParts[^1] = request.Slug.Value;
            }

            if (request.ParentCategoryId.IsSet)
            {
                category.ParentCategoryId = request.ParentCategoryId.Value;

                // Parent update requires Path update
                pathParts ??= category.Path.ToString().Split('.').ToList();
                pathParts = newParentPath is not null
                    ? newParentPath.Split('.').Concat([pathParts[^1]]).ToList()
                    : [pathParts[^1]];
            }

            if (request.ImagePath.IsSet)
                category.ImagePath = request.ImagePath.Value;

            if (request.DisplayOrder.IsSet)
                category.DisplayOrder = request.DisplayOrder.Value;

            if (request.IsActive.IsSet)
            {
                // ARCHITECTURAL DECISION: Deactivating a category will not cascade to its descendants
                category.IsActive = request.IsActive.Value;
            }

            // Apply a new Path if created
            if (pathParts is not null)
            {
                category.Path = new LTree(string.Join(".", pathParts));
            }

            categoryRepository.UpdateCategory(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var categoryExists = await queryProvider.CategoryExistsAsync(categoryId, cancellationToken);
            return categoryExists switch
            {
                // Modified concurrently by another request
                true => ApiResults.Conflict(CategoryValidation.ErrorCodes.UpdateConcurrencyConflict),
                // Deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        var response = new UpdateCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Slug = category.CurrentSlug,
            ImagePath = category.ImagePath,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            Path = category.Path.ToString(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            DeletedAt = category.DeletedAt,
            RowVersion = category.RowVersion
        };

        logger.LogInformation("Category with ID {Id} has been updated with changes: {@Changed}",
            response.Id,
            new
            {
                Name = request.Name.IsSet,
                Slug = request.Slug.IsSet,
                ParentCategoryId = request.ParentCategoryId.IsSet,
                ImagePath = request.ImagePath.IsSet,
                DisplayOrder = request.DisplayOrder.IsSet,
                IsActive = request.IsActive.IsSet
            });

        return ApiResults.Ok(response);
    }
}
