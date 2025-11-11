using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.RestoreProduct;

public static class RestoreProductEndpoint
{
    internal static IEndpointRouteBuilder MapRestoreProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:restore", HandleAsync)
            .WithName(ProductEndpointsModule.Names.RestoreProduct);

        return app;
    }

    private static async Task<Results<OkResult<RestoreProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        RestoreProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(productId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<OkResult<RestoreProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(RestoreProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
