using ForeverBloom.Api.Contracts.Common;
using Microsoft.AspNetCore.Http.Features;

namespace ForeverBloom.Api.Helpers;

public static class ProblemDetailsHelper
{
    /// <summary>
    /// Creates a ProblemDetails object with a status code, error type, and an optional error code.
    /// </summary>
    /// <param name="statusCode">The status code of the HTTP response.</param>
    /// <param name="errorCode">An optional error code to include in the ProblemDetails extensions.</param>
    /// <returns>A ProblemDetails object containing type, title, status, and optional error code information.</returns>
    public static ProblemDetails CreateProblemDetails(int statusCode, string? errorCode = null)
    {
        var problemDetails = new ProblemDetails();
        ApplyProblemDetailsDefaults(problemDetails, statusCode);

        if (errorCode is not null)
        {
            problemDetails.Extensions.TryAdd("errorCode", errorCode);
        }

        return problemDetails;
    }

    /// <summary>
    /// Creates an HttpValidationProblemDetails object with a status code and a collection of validation errors.
    /// </summary>
    /// <param name="statusCode">The status code of the HTTP response.</param>
    /// <param name="errors">A dictionary containing validation errors where the key represents the field, and the value is an array of error messages.</param>
    /// <returns>An HttpValidationProblemDetails object containing type, title, status, and validation error details.</returns>
    public static ValidationProblemDetails CreateValidationProblemDetails(int statusCode,
      IDictionary<string, string[]> errors)
    {
        var problemDetails = new ValidationProblemDetails(errors);
        ApplyProblemDetailsDefaults(problemDetails, statusCode);

        return problemDetails;
    }

    private static void ApplyProblemDetailsDefaults(ProblemDetails problemDetails, int statusCode)
    {
        problemDetails.Status = statusCode;
        problemDetails.Type = GetType(statusCode);
        problemDetails.Title = GetTitle(statusCode);
    }

    /// <summary>
    /// Enriches a ProblemDetails object with instance, requestId, and traceId from the HttpContext.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails object to enrich.</param>
    /// <param name="httpContext">The current HttpContext.</param>
    public static void EnrichProblemDetails(ProblemDetails problemDetails, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(httpContext);

        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        problemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);

        var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        problemDetails.Extensions.TryAdd("traceId", activity?.Id);
    }

    /// <summary>
    /// Enriches a ProblemDetails object with instance, requestId, and traceId from the HttpContext.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails object to enrich.</param>
    /// <param name="httpContext">The current HttpContext.</param>
    public static void EnrichProblemDetails(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(httpContext);

        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        problemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);

        var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        problemDetails.Extensions.TryAdd("traceId", activity?.Id);
    }

    private static string GetType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        406 => "https://tools.ietf.org/html/rfc9110#section-15.5.7",
        408 => "https://tools.ietf.org/html/rfc9110#section-15.5.9",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        412 => "https://tools.ietf.org/html/rfc9110#section-15.5.13",
        415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
        426 => "https://tools.ietf.org/html/rfc9110#section-15.5.22",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
        // Default case for any other status code
        _ => "https://tools.ietf.org/html/rfc9110#section-15"
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        406 => "Not Acceptable",
        408 => "Request Timeout",
        409 => "Conflict",
        412 => "Precondition Failed",
        415 => "Unsupported Media Type",
        422 => "Unprocessable Entity",
        426 => "Upgrade Required",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        504 => "Gateway Timeout",
        // Default case for any other status code
        _ => "Error"
    };
}
