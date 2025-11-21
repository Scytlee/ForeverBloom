using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.CreateCategory;

public static class CreateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.CreateCategory);

        return app;
    }

    private static async Task<Results<CreatedResult<CreateCategoryResponse>, ConflictResult, BadRequestResult>> HandleAsync(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand();
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<CreatedResult<CreateCategoryResponse>, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Created(
                $"/api/v1/admin/categories/{request.Slug}",
                CreateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                CategoryErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                CategoryErrors.NameNotUniqueWithinParent nameConflict => ApiResults.Conflict(nameConflict.Code),
                CategoryErrors.ParentNotFound parentError => ApiResults.BadRequest(parentError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
