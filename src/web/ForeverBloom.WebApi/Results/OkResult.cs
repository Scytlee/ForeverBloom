using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;

namespace ForeverBloom.WebApi.Results;

public sealed class OkResult<T> : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<T>
{
    public T? Value { get; }

    object? IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status200OK;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal OkResult(T? value)
    {
        Value = value;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode;

        var jsonOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value;
        var jsonSerializerOptions = jsonOptions?.SerializerOptions ?? JsonSerializerOptions.Default;

        return httpContext.Response.WriteAsJsonAsync(Value, jsonSerializerOptions, ContentTypes.Json);
    }

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(T), [ContentTypes.Json]));
    }
}
