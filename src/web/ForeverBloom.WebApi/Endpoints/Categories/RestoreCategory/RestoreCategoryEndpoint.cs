using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppCategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.WebApi.Endpoints.Categories.RestoreCategory;

public static class RestoreCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapRestoreCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:restore", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.RestoreCategory);

        return app;
    }

    private static async Task<Results<OkResult<RestoreCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        RestoreCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(categoryId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<OkResult<RestoreCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(RestoreCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppCategoryErrors.NotFound => ApiResults.NotFound(),
                AppCategoryErrors.HasArchivedAncestors => ApiResults.BadRequest(error),
                AppCategoryErrors.TooManyDescendants => ApiResults.BadRequest(error),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
