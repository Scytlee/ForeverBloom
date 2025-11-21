using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.CreateProduct;

public static class CreateProductEndpoint
{
    internal static IEndpointRouteBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.CreateProduct);

        return app;
    }

    private static async Task<Results<CreatedResult<CreateProductResponse>, ConflictResult, BadRequestResult>> HandleAsync(
        CreateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var commandResult = request.ToCommand();
        if (commandResult.IsFailure)
        {
            return ApiResults.BadRequest(commandResult.Error);
        }

        var result = await sender.Send(commandResult.Value, cancellationToken);

        return result.Match<Results<CreatedResult<CreateProductResponse>, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Created(
                $"/api/v1/admin/products/{request.Slug}",
                CreateProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                ProductErrors.CategoryNotFound categoryError => ApiResults.BadRequest(categoryError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
