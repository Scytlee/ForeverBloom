using ForeverBloom.Application.Products.Queries.GetProductById;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.GetProductById;

public static class GetProductByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetProductByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.GetProductById);

        return app;
    }

    internal static async Task<Results<OkResult<GetProductByIdResponse>, NotFoundResult, BadRequestResult>> HandleAsync(
        long id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var queryResult = GetProductByIdQuery.Create(id);
        if (queryResult.IsFailure)
        {
            return ApiResults.BadRequest(queryResult.Error);
        }

        var result = await sender.Send(queryResult.Value, cancellationToken);

        return result.Match<Results<OkResult<GetProductByIdResponse>, NotFoundResult, BadRequestResult>>(
            onSuccess: product => ApiResults.Ok(GetProductByIdResponse.FromResult(product)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                _ => ApiResults.BadRequest(error)
            });
    }
}
