using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.DeleteProduct;

public static class DeleteProductEndpoint
{
    internal static IEndpointRouteBuilder MapDeleteProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{productId:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.DeleteProduct);

        return app;
    }

    private static async Task<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        [AsParameters] DeleteProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(productId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<NoContentResult, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: () => ApiResults.NoContent(),
            onFailure: error => error switch
            {
                ProductErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ProductErrors.CannotDeleteNotArchived => ApiResults.BadRequest(error),
                ProductErrors.CannotDeleteTooSoon => ApiResults.BadRequest(error),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
