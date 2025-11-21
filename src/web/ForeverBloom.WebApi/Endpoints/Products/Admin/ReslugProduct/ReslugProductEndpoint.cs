using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ReslugProduct;

public static class ReslugProductEndpoint
{
    internal static IEndpointRouteBuilder MapReslugProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:reslug", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ReslugProduct);

        return app;
    }

    private static async Task<Results<OkResult<ReslugProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        ReslugProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ReslugProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ReslugProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ProductErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
