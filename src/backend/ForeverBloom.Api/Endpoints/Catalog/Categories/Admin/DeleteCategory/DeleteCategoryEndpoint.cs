using ForeverBloom.Api.Contracts.Catalog.Categories.Admin.DeleteCategory;
using ForeverBloom.Api.EndpointFilters;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Categories.Admin.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    public static IServiceCollection AddDeleteCategoryEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IDeleteCategoryEndpointQueryProvider, DeleteCategoryEndpointQueryProvider>();

        return services;
    }

    public static IEndpointRouteBuilder MapDeleteCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{categoryId:int}", HandleAsync)
            .ValidateRequest<DeleteCategoryRequest>()
            .UseUnitOfWork()
            .WithName(CategoryEndpointsGroup.Names.DeleteCategory);

        return app;
    }

    internal static async Task<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult>> HandleAsync(
        int categoryId,
        [AsParameters] DeleteCategoryRequest request,
        IDeleteCategoryEndpointQueryProvider queryProvider,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (categoryId <= 0)
        {
            return ApiResults.ValidationProblem(nameof(categoryId),
                CategoryValidation.ErrorCodes.IdInvalid);
        }


        // Validate all four rules in a single database roundtrip
        var validation = await queryProvider.ValidateCategoryForDeletionAsync(categoryId, cancellationToken);

        if (!validation.Exists)
        {
            return ApiResults.NotFound();
        }

        if (!validation.IsArchived)
        {
            return ApiResults.ValidationProblem(nameof(categoryId),
                CategoryValidation.ErrorCodes.CategoryNotArchived);
        }

        if (validation.HasChildCategories)
        {
            return ApiResults.ValidationProblem(nameof(categoryId),
                CategoryValidation.ErrorCodes.CategoryHasChildCategories);
        }

        if (validation.HasProducts)
        {
            return ApiResults.ValidationProblem(nameof(categoryId),
                CategoryValidation.ErrorCodes.CategoryHasProducts);
        }

        try
        {
            categoryRepository.DeleteCategory(categoryId, request.RowVersion!.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var newValidation = await queryProvider.ValidateCategoryForDeletionAsync(categoryId, cancellationToken);
            return newValidation.Exists switch
            {
                true => ApiResults.Conflict(CategoryValidation.ErrorCodes.DeleteConcurrencyConflict),
                false => ApiResults.NotFound()
            };
        }

        logger.LogInformation("Category with ID {Id} has been permanently deleted", categoryId);
        return ApiResults.NoContent();
    }
}
