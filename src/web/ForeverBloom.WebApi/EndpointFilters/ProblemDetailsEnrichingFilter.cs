using ForeverBloom.WebApi.Helpers;
using ForeverBloom.WebApi.Models;

namespace ForeverBloom.WebApi.EndpointFilters;

internal sealed class ProblemDetailsEnrichingFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
      EndpointFilterInvocationContext context,
      EndpointFilterDelegate next)
    {
        var result = await next(context);

        var unwrappedResult = TryUnwrapResult(result);
        switch (unwrappedResult)
        {
            case IValueHttpResult { Value: ProblemDetails problemDetails }:
                ProblemDetailsHelper.EnrichProblemDetails(problemDetails, context.HttpContext);
                break;
            case IValueHttpResult { Value: Microsoft.AspNetCore.Mvc.ProblemDetails mvcProblemDetails }:
                ProblemDetailsHelper.EnrichProblemDetails(mvcProblemDetails, context.HttpContext);
                break;
        }

        return result;
    }

    private static IResult? TryUnwrapResult(object? result) => result switch
    {
        INestedHttpResult { Result: { } r } => r,
        IResult r => r,
        _ => null
    };
}

public sealed record EnrichesProblemDetails;

public static class ProblemDetailsEnrichingFilterExtensions
{
    public static TBuilder EnrichProblemDetails<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter<TBuilder, ProblemDetailsEnrichingFilter>()
          .WithMetadata(new EnrichesProblemDetails());

        return builder;
    }
}
