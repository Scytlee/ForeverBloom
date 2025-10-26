using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.RestoreCategory;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.RestoreCategory;

public static class RestoreCategoryEndpoint
{
    public static IServiceCollection AddRestoreCategoryEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IRestoreCategoryEndpointQueryProvider, RestoreCategoryEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapRestoreCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:int}/restore", HandleAsync)
            .ValidateRequest<RestoreCategoryRequest>()
            .UseUnitOfWork()
            .WithName(CategoryEndpointsGroup.Names.RestoreCategory);

        return app;
    }

    internal static async Task<Results<OkResult<RestoreCategoryResponse>, NotFoundResult, ConflictResult>> HandleAsync(
        int categoryId,
        RestoreCategoryRequest request,
        IRestoreCategoryEndpointQueryProvider queryProvider,
        ICategoryRepository categoryRepository,
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
            return ApiResults.Conflict(CategoryValidation.ErrorCodes.RestoreConcurrencyConflict);
        }

        try
        {
            categoryRepository.RestoreCategory(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var categoryStillExists = await queryProvider.CategoryExistsAsync(categoryId, cancellationToken);
            return categoryStillExists switch
            {
                // Category was modified concurrently by another request
                true => ApiResults.Conflict(CategoryValidation.ErrorCodes.RestoreConcurrencyConflict),
                // Category was deleted concurrently by another request
                false => ApiResults.NotFound()
            };
        }

        var response = new RestoreCategoryResponse
        {
            DeletedAt = null,
            RowVersion = category.RowVersion
        };

        logger.LogInformation("Category with ID {Id} has been restored", category.Id);
        return ApiResults.Ok(response);
    }
}
