using ForeverBloom.Application.Categories.Queries.GetCategoryById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.GetCategoryById;

public static class GetCategoryByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.GetCategoryById);

        return app;
    }

    internal static async Task<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult, BadRequestResult>> HandleAsync(
        long id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = GetCategoryByIdQuery.Create(id);
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<GetCategoryByIdResponse>, NotFoundResult, BadRequestResult>>(
            onSuccess: category => ApiResults.Ok(GetCategoryByIdResponse.FromResult(category)),
            onFailure: error => error switch
            {
                CategoryErrors.NotFoundById => ApiResults.NotFound(),
                _ => ApiResults.BadRequest(error)
            });
    }
}
