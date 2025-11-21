using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ArchiveCategory;

public static class ArchiveCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapArchiveCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:archive", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ArchiveCategory);

        return app;
    }

    private static async Task<Results<OkResult<ArchiveCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ArchiveCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ArchiveCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ArchiveCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                CategoryErrors.TooManyDescendants => ApiResults.BadRequest(error),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
