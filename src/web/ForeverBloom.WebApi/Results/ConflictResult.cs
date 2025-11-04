using System.Reflection;
using System.Text.Json;
using ForeverBloom.WebApi.Helpers;
using ForeverBloom.WebApi.Models;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ForeverBloom.WebApi.Results;

public sealed class ConflictResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<ProblemDetails>
{
    public ProblemDetails Value { get; }

    object? IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status409Conflict;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal ConflictResult(string errorCode)
    {
        Value = ProblemDetailsHelper.CreateProblemDetails(StatusCode, errorCode);
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode;

        var jsonOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value;
        var jsonSerializerOptions = jsonOptions?.SerializerOptions ?? JsonSerializerOptions.Default;

        return httpContext.Response.WriteAsJsonAsync(Value, jsonSerializerOptions, ContentTypes.ProblemJson);
    }

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(ProblemDetails), [ContentTypes.ProblemJson]));
    }
}
