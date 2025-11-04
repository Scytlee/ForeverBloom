using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Products;
using ForeverBloom.WebApi.Mapping;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.CreateProduct;

public static class CreateProductEndpoint
{
    internal static IEndpointRouteBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.CreateProduct);

        return app;
    }

    private static async Task<Results<CreatedResult<CreateProductResponse>, ValidationProblemResult, ConflictResult, BadRequestResult>> HandleAsync(
        CreateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Validate and convert availability status string to domain object
        if (!AvailabilityStatusMapper.TryParse(request.AvailabilityStatus, out var availabilityStatus))
        {
            return ApiResults.ValidationProblem(nameof(request.AvailabilityStatus), new ValidationErrorDetail(
                code: "ProductAvailabilityStatus.InvalidValue",
                message: "The provided availability status is not defined.",
                attemptedValue: request.AvailabilityStatus,
                customState: new { AvailabilityStatusMapper.ValidValues }));
        }

        var result = await sender.Send(request.ToCommand(availabilityStatus!), cancellationToken);

        return result.Match<Results<CreatedResult<CreateProductResponse>, ValidationProblemResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Created(
                $"/api/v1/admin/products/{request.Slug}",
                CreateProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ProductErrors.SlugNotAvailable slugError => ApiResults.Conflict(slugError.Code),
                ProductErrors.CategoryNotFound categoryError => ApiResults.BadRequest(categoryError),
                ValidationError validationError => ApiResults.ValidationProblem(validationError),
                _ => ApiResults.BadRequest(error)
            });
    }
}
