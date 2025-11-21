using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.Admin.ListCategories;

public static class ListCategoriesEndpoint
{
    internal static IEndpointRouteBuilder MapListCategoriesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ListCategories);

        return app;
    }

    private static async Task<Results<OkResult<ListCategoriesResponse>, BadRequestResult>> HandleAsync(
        [AsParameters] ListCategoriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = request.ToQuery();
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ListCategoriesResponse>, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ListCategoriesResponse.FromResult(payload)),
            onFailure: error => ApiResults.BadRequest(error));
    }
}
