using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Categories;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapDeleteCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{categoryId:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.DeleteCategory);

        return app;
    }

    private static async Task<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        [AsParameters] DeleteCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(categoryId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: () => ApiResults.NoContent(),
            onFailure: error => error switch
            {
                CategoryErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                CategoryErrors.CannotDeleteNotArchived => ApiResults.BadRequest(error),
                CategoryErrors.CannotDeleteTooSoon => ApiResults.BadRequest(error),
                CategoryErrors.CannotDeleteHasChildren => ApiResults.BadRequest(error),
                CategoryErrors.CannotDeleteHasProducts => ApiResults.BadRequest(error),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
