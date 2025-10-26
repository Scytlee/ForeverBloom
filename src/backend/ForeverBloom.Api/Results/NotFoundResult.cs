using System.Reflection;
using System.Text.Json;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Helpers;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ForeverBloom.Api.Results;

public sealed class NotFoundResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<ProblemDetails>
{
    public ProblemDetails? Value { get; }

    object? IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status404NotFound;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal NotFoundResult()
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

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(ProblemDetails), [ContentTypes.ProblemJson]));
    }
}
