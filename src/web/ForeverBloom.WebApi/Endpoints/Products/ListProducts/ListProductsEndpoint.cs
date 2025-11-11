using ForeverBloom.Application.Abstractions.Validation;
using ForeverBloom.Application.Sorting;
using ForeverBloom.WebApi.Parsing;
using ForeverBloom.WebApi.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ForeverBloom.WebApi.Endpoints.Products.ListProducts;

public static class ListProductsEndpoint
{
    internal static IEndpointRouteBuilder MapListProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", HandleAsync)
            .WithName(ProductEndpointsModule.Names.ListProducts);

        return app;
    }

    private static async Task<Results<OkResult<ListProductsResponse>, ValidationProblemResult, BadRequestResult>> HandleAsync(
        [AsParameters] ListProductsRequest request,
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

        var query = request.ToQuery(sortProperties);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<Results<OkResult<ListProductsResponse>, ValidationProblemResult, BadRequestResult>>(
            onSuccess: payload => ApiResults.Ok(ListProductsResponse.FromResult(payload)),
            onFailure: error => error switch
            {
                ValidationError validation => ApiResults.ValidationProblem(validation),
                _ => ApiResults.BadRequest(error)
            });
    }
}
