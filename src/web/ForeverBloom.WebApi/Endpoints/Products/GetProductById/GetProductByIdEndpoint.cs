using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.Application.Products.Queries.GetProductById;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.GetProductById;

public static class GetProductByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetProductByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.GetProductById);

        return app;
    }

    internal static async Task<Results<OkResult<GetProductByIdResponse>, NotFoundResult, ValidationProblemResult, BadRequestResult>> HandleAsync(
        long id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetProductByIdQuery(id),
            cancellationToken);

        return result.Match<Results<OkResult<GetProductByIdResponse>, NotFoundResult, ValidationProblemResult, BadRequestResult>>(
            onSuccess: product => ApiResults.Ok(GetProductByIdResponse.FromResult(product)),
            onFailure: error => error switch
            {
                ProductErrors.NotFound => ApiResults.NotFound(),
                ValidationError validationError => ApiResults.ValidationProblem(validationError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
