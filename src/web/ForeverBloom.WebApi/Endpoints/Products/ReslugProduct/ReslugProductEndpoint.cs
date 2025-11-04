using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.ReslugProduct;

public static class ReslugProductEndpoint
{
    internal static IEndpointRouteBuilder MapReslugProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:reslug", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ReslugProduct);

        return app;
    }

    private static async Task<Results<OkResult<ReslugProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        ReslugProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(productId), cancellationToken);

        return result.Match<Results<OkResult<ReslugProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReslugProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ProductErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
