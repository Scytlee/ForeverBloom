using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.WebApi.Mapping;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using AppCategoryErrors = ForeverBloom.Application.Categories.CategoryErrors;

namespace ForeverBloom.WebApi.Endpoints.Categories.UpdateCategory;

public static class UpdateCategoryEndpoint
{
    internal static IEndpointRouteBuilder MapUpdateCategoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/{categoryId:long}", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.UpdateCategory);

        return app;
    }

    private static async Task<Results<OkResult<UpdateCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>> HandleAsync(
        long categoryId,
        UpdateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
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

        var result = await sender.Send(request.ToCommand(categoryId, publishStatus), cancellationToken);

        return result.Match<Results<OkResult<UpdateCategoryResponse>, ValidationProblemResult, NotFoundResult, ConflictResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(UpdateCategoryResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                AppCategoryErrors.NotFound => ApiResults.NotFound(),
                ApplicationErrors.ConcurrencyConflict concurrency => ApiResults.Conflict(concurrency.Code),
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
