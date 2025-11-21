using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapDeleteCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{categoryId:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.DeleteCategory);

        return app;
    }

    private static async Task<Results<NoContentResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        [AsParameters] DeleteCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(categoryId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<NoContentResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: () => ApiResults.NoContent(),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
