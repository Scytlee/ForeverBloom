using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Sorting;
using ForeverBloom.WebApi.Mapping;
using ForeverBloom.WebApi.Models;
using ForeverBloom.WebApi.Parsing;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Categories.ListCategories;

public static class ListCategoriesEndpoint
{
    internal static IEndpointRouteBuilder MapListCategoriesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(CategoryEndpointsModule.Names.ListCategories);

        return app;
    }

    private static async Task<Results<OkResult<ListCategoriesResponse>, ValidationProblemResult, BadRequestResult>> HandleAsync(
        [AsParameters] ListCategoriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // HTTP-specific validation: parse sortBy query parameter
        SortProperty[]? sortProperties = null;
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            if (!SortPropertyParser.TryParse(request.SortBy, out sortProperties, out var error))
            {
                return ApiResults.ValidationProblem(nameof(request.SortBy), error);
            }
        }

        // HTTP-specific validation: parse publishStatus query parameter
        int? publishStatus = null;
        if (!string.IsNullOrWhiteSpace(request.PublishStatus))
        {
            if (!PublishStatusMapper.TryParse(request.PublishStatus, out var parsedStatus))
            {
                return ApiResults.ValidationProblem(nameof(request.PublishStatus), new ValidationErrorDetail(
                    code: "CategoryPublishStatus.InvalidValue",
                    message: "The provided publish status is not defined.",
                    attemptedValue: request.PublishStatus,
                    customState: new { PublishStatusMapper.ValidValues }));
            }
            publishStatus = parsedStatus.Code;
        }

        var query = request.ToQuery(sortProperties, publishStatus);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<ListCategoriesResponse>, ValidationProblemResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ListCategoriesResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
