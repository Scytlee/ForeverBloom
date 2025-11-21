using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProduct;

public static class UpdateProductEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{productId:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.UpdateProduct);

        return app;
    }

    private static async Task<Results<OkResult<UpdateProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        UpdateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<UpdateProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ProductErrors.CategoryNotFound categoryError => ApiResults.BadRequest(categoryError),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
