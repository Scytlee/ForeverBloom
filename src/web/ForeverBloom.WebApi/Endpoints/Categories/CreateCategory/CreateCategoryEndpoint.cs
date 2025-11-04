using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Categories;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.CreateCategory;

public static class CreateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.CreateCategory);

        return app;
    }

    private static async Task<Results<CreatedResult<CreateCategoryResponse>, ValidationProblemResult, ConflictResult, BadRequestResult>> HandleAsync(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);

        return result.Match<Results<CreatedResult<CreateCategoryResponse>, ValidationProblemResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Created(
                $"/api/v1/admin/categories/{request.Slug}",
                CreateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                CategoryErrors.NameNotUniqueWithinParent nameConflict => ApiResults.Conflict(nameConflict.Code),
                CategoryErrors.ParentNotFound parentError => ApiResults.BadRequest(parentError),
                ValidationError validationError => ApiResults.ValidationProblem(validationError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
