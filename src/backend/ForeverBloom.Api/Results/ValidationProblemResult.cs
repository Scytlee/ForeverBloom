using System.Reflection;
using System.Text.Json;
using ForeverBloom.Api.Contracts.Common;
using ForeverBloom.Api.Helpers;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ForeverBloom.Api.Results;

public sealed class ValidationProblemResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<ValidationProblemDetails>
{
    public ValidationProblemDetails Value { get; }

    object IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status400BadRequest;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal ValidationProblemResult(IDictionary<string, string[]> errors)
    {
        Value = ProblemDetailsHelper.CreateValidationProblemDetails(StatusCode, errors);
    }

    internal ValidationProblemResult(string propertyName, string errorCode)
    {
        var errors = new Dictionary<string, string[]> { { propertyName, [errorCode] } };
        Value = ProblemDetailsHelper.CreateValidationProblemDetails(StatusCode, errors);
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

        builder.Metadata.Add(new ProducesResponseTypeMetadata(
            StatusCodes.Status400BadRequest,
            typeof(HttpValidationProblemDetails),
            [ContentTypes.ProblemJson]));
    }
}
