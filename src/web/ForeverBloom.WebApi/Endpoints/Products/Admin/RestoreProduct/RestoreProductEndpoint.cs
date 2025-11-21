using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.RestoreProduct;

public static class RestoreProductEndpoint
{
    internal static IEndpointRouteBuilder MapRestoreProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:restore", HandleAsync)
            .WithName(ProductEndpointsModule.Names.RestoreProduct);

        return app;
    }

    private static async Task<Results<OkResult<RestoreProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        RestoreProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<RestoreProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(RestoreProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
