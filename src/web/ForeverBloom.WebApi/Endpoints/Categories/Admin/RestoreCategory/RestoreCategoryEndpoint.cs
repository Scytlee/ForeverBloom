using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.RestoreCategory;

public static class RestoreCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapRestoreCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:restore", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.RestoreCategory);

        return app;
    }

    private static async Task<Results<OkResult<RestoreCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        RestoreCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<RestoreCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(RestoreCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                CategoryErrors.HasArchivedAncestors => ApiResults.BadRequest(error),
                CategoryErrors.TooManyDescendants => ApiResults.BadRequest(error),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
