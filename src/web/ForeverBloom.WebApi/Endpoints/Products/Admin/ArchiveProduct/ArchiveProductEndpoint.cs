using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ArchiveProduct;

public static class ArchiveProductEndpoint
{
    internal static IEndpointRouteBuilder MapArchiveProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:archive", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ArchiveProduct);

        return app;
    }

    private static async Task<Results<OkResult<ArchiveProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        ArchiveProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand(productId);
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<OkResult<ArchiveProductResponse>, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ArchiveProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                _ => ApiResults.BadRequest(error)
            });
    }
}
