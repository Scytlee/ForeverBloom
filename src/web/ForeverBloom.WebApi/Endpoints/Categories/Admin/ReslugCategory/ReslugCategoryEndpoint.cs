using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ReslugCategory;

public static class ReslugCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapReslugCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:reslug", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ReslugCategory);

        return app;
    }

    private static async Task<Results<OkResult<ReslugCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ReslugCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ReslugCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReslugCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                CategoryErrors.SlugNotAvailable => ApiResults.Conflict(error.Code),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
