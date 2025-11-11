using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppCategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.WebApi.Endpoints.Categories.ArchiveCategory;

public static class ArchiveCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapArchiveCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:archive", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ArchiveCategory);

        return app;
    }

    private static async Task<Results<OkResult<ArchiveCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ArchiveCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(categoryId), cancellationToken);

        return result.Match<Results<OkResult<ArchiveCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ArchiveCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppCategoryErrors.NotFound => ApiResults.NotFound(),
                AppCategoryErrors.TooManyDescendants => ApiResults.BadRequest(error),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
