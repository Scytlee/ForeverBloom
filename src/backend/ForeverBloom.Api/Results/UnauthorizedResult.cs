using System.Text.Json;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Helpers;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ForeverBloom.Api.Results;

public sealed class UnauthorizedResult : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<ProblemDetails>
{
    public ProblemDetails Value { get; }

    object? IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status401Unauthorized;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal UnauthorizedResult()
    {
        Value = ProblemDetailsHelper.CreateProblemDetails(StatusCode);
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode;

        var jsonOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value;
        var jsonSerializerOptions = jsonOptions?.SerializerOptions ?? JsonSerializerOptions.Default;

        return httpContext.Response.WriteAsJsonAsync(Value, jsonSerializerOptions, ContentTypes.ProblemJson);
    }
}
