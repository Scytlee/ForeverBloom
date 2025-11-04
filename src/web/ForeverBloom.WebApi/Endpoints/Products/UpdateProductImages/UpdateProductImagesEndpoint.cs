using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProductImages;

public static class UpdateProductImagesEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateProductImagesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{productId:long}/images", HandleAsync)
            .WithName(ProductEndpointsModule.Names.UpdateProductImages);

        return app;
    }

    private static async Task<Results<OkResult<UpdateProductImagesResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        UpdateProductImagesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(productId), cancellationToken);

        return result.Match<Results<OkResult<UpdateProductImagesResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateProductImagesResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
