using System.Reflection;
using System.Text.Json;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Helpers;
using ForeverBloom.WebApi.Models;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using IResult = Microsoft.AspNetCore.Http.IResult;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace ForeverBloom.WebApi.Results;

/// <summary>
/// Represents a 400 Bad Request result containing errors.
/// </summary>
public sealed class BadRequestResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<BadRequestProblemDetails>
{
    public BadRequestProblemDetails Value { get; }

    object IValueHttpResult.Value => Value;

    public int StatusCode => StatusCodes.Status400BadRequest;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal BadRequestResult(IError error)
    {
        ErrorDetail[] errors;

        if (error is CompositeError composite)
        {
            // Unwrap composite error: convert each inner error to ErrorDetail
            errors = composite.Errors.Select(ErrorDetail.FromError).ToArray();
        }
        else
        {
            // Single error becomes single-item array
            errors = [ErrorDetail.FromError(error)];
        }

        Value = ProblemDetailsHelper.CreateBadRequestProblemDetails(StatusCode, errors);
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
            typeof(BadRequestProblemDetails),
            [ContentTypes.ProblemJson]));
    }
}
