using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.ArchiveCategory;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.ArchiveCategory;

public static class ArchiveCategoryEndpoint
{
    public static IServiceCollection AddArchiveCategoryEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IArchiveCategoryEndpointQueryProvider, ArchiveCategoryEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapArchiveCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:int}/archive", HandleAsync)
            .ValidateRequest<ArchiveCategoryRequest>()
            .UseUnitOfWork()
            .WithName(CategoryEndpointsGroup.Names.ArchiveCategory);

        return app;
    }

    internal static async Task<Results<OkResult<ArchiveCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult>> HandleAsync(
        int categoryId,
        ArchiveCategoryRequest request,
        IArchiveCategoryEndpointQueryProvider queryProvider,
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
            return ApiResults.Conflict(CategoryValidation.ErrorCodes.ArchiveConcurrencyConflict);
        }

        if (await queryProvider.HasChildCategoriesAsync(categoryId, cancellationToken))
        {
            return ApiResults.ValidationProblem("CategoryId",
                CategoryValidation.ErrorCodes.CannotArchiveCategoryWithChildren);
        }

        try
        {
            categoryRepository.ArchiveCategory(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var categoryStillExists = await queryProvider.CategoryExistsAsync(categoryId, cancellationToken);
            return categoryStillExists switch
            {
                // Category was deleted concurrently by another request
                false => ApiResults.NotFound(),
                // Category was modified concurrently by another request
                true => ApiResults.Conflict(CategoryValidation.ErrorCodes.ArchiveConcurrencyConflict)
            };
        }

        var response = new ArchiveCategoryResponse
        {
            DeletedAt = category.DeletedAt!.Value,
            RowVersion = category.RowVersion
        };

        logger.LogInformation("Category with ID {Id} has been archived at {DeletedAt:o}", category.Id, response.DeletedAt);
        return ApiResults.Ok(response);
    }
}
