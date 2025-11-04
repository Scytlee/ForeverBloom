using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppCategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.WebApi.Endpoints.Categories.ReslugCategory;

public static class ReslugCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapReslugCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{categoryId:long}:reslug", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ReslugCategory);

        return app;
    }

    private static async Task<Results<OkResult<ReslugCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        ReslugCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(categoryId), cancellationToken);

        return result.Match<Results<OkResult<ReslugCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReslugCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppCategoryErrors.NotFoundById => ApiResults.NotFound(),
                AppCategoryErrors.SlugNotAvailable => ApiResults.Conflict(error.Code),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
