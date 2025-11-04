using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppCategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReparentCategory;

public static class ReparentCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapReparentCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:reparent", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ReparentCategory);

        return app;
    }

    private static async Task<Results<OkResult<ReparentCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ReparentCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(categoryId), cancellationToken);

        return result.Match<Results<OkResult<ReparentCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReparentCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppCategoryErrors.NotFound => ApiResults.NotFound(),
                AppCategoryErrors.ParentNotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
