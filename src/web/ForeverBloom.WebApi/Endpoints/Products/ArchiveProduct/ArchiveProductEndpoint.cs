using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.ArchiveProduct;

public static class ArchiveProductEndpoint
{
    internal static IEndpointRouteBuilder MapArchiveProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{productId:long}:archive", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ArchiveProduct);

        return app;
    }

    private static async Task<Results<OkResult<ArchiveProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        ArchiveProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(productId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<Results<OkResult<ArchiveProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ArchiveProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.NotFoundById => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
