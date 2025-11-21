using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.DeleteProduct;

public static class DeleteProductEndpoint
{
    internal static IEndpointRouteBuilder MapDeleteProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/{productId:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.DeleteProduct);

        return app;
    }

    private static async Task<Results<NoContentResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        [AsParameters] DeleteProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<NoContentResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: () => ApiResults.NoContent(),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
