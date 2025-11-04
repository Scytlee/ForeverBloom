using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Categories;
using ForeverBloom.Application.Categories.Queries.GetCategoryById;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.GetCategoryById;

public static class GetCategoryByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoryById);

        return app;
    }

    internal static async Task<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult, ValidationProblemResult, BadRequestResult>> HandleAsync(
        long id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCategoryByIdQuery(id),
            cancellationToken);

        return result.Match<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult, ValidationProblemResult, BadRequestResult>>(
            onSuccess: category => ApiResults.Ok(GetCategoryByIdResponse.FromResult(category)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFound => ApiResults.NotFound(),
                ValidationError validationError => ApiResults.ValidationProblem(validationError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
