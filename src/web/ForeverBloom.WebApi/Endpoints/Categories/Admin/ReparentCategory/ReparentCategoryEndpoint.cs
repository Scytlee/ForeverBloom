using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ReparentCategory;

public static class ReparentCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapReparentCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:reparent", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ReparentCategory);

        return app;
    }

    private static async Task<Results<OkResult<ReparentCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ReparentCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ReparentCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReparentCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                CategoryErrors.ParentNotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
