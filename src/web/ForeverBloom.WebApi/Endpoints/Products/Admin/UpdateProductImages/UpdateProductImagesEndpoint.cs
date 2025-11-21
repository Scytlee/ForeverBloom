using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.UpdateProductImages;

public static class UpdateProductImagesEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateProductImagesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{productId:long}/images", HandleAsync)
            .WithName(ProductEndpointsModule.Names.UpdateProductImages);

        return app;
    }

    private static async Task<Results<OkResult<UpdateProductImagesResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        UpdateProductImagesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<UpdateProductImagesResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateProductImagesResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
