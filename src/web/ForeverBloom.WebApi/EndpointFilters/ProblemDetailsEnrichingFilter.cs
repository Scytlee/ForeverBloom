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

        if (result is IValueHttpResult { Value: ProblemDetails problemDetails })
        {
            ProblemDetailsHelper.EnrichProblemDetails(problemDetails, context.HttpContext);
        }

        if (result is IValueHttpResult { Value: Microsoft.AspNetCore.Mvc.ProblemDetails mvcProblemDetails })
        {
            ProblemDetailsHelper.EnrichProblemDetails(mvcProblemDetails, context.HttpContext);
        }

        return result;
    }
}

public sealed record ProblemDetailsEnriched;

public static class ProblemDetailsEnrichingFilterExtensions
{
    public static TBuilder EnrichProblemDetails<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter<TBuilder, ProblemDetailsEnrichingFilter>()
          .WithMetadata(new ProblemDetailsEnriched());

        return builder;
    }
}
