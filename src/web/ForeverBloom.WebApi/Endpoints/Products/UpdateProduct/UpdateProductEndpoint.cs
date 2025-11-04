using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Mapping;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppProductErrors = ForeverBloom.Application.Products.ProductErrors;

namespace ForeverBloom.WebApi.Endpoints.Products.UpdateProduct;

public static class UpdateProductEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{productId:long}", HandleAsync)
            .WithName(ProductEndpointsModule.Names.UpdateProduct);

        return app;
    }

    private static async Task<Results<OkResult<UpdateProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long productId,
        UpdateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Validate and convert availability status string if provided
        ProductAvailabilityStatus? availabilityStatus = null;
        if (request.Availability.IsSet)
        {
            if (!AvailabilityStatusMapper.TryParse(request.Availability.Value, out var parsedStatus))
            {
                return ApiResults.ValidationProblem(
                    nameof(request.Availability),
                    new ValidationErrorDetail(
                        code: "ProductAvailabilityStatus.InvalidValue",
                        message: "The provided availability status is not defined.",
                        attemptedValue: request.Availability.Value,
                        customState: new { AvailabilityStatusMapper.ValidValues }));
            }

            availabilityStatus = parsedStatus;
        }

        // Validate and convert publish status string if provided
        PublishStatus? publishStatus = null;
        if (request.PublishStatus.IsSet)
        {
            if (!PublishStatusMapper.TryParse(request.PublishStatus.Value, out var parsedPublishStatus))
            {
                return ApiResults.ValidationProblem(
                    nameof(request.PublishStatus),
                    new ValidationErrorDetail(
                        code: "PublishStatus.InvalidValue",
                        message: "The provided publish status is not defined.",
                        attemptedValue: request.PublishStatus.Value,
                        customState: new { PublishStatusMapper.ValidValues }));
            }

            publishStatus = parsedPublishStatus;
        }

        var result = await sender.Send(request.ToCommand(productId, availabilityStatus, publishStatus), cancellationToken);

        return result.Match<Results<OkResult<UpdateProductResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateProductResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppProductErrors.NotFound => ApiResults.NotFound(),
                AppProductErrors.CategoryNotFound categoryError => ApiResults.BadRequest(categoryError),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
