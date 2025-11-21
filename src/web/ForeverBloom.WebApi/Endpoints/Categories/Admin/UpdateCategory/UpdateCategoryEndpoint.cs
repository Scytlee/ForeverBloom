using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{categoryId:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.UpdateCategory);

        return app;
    }

    private static async Task<Results<OkResult<UpdateCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<UpdateCategoryResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
