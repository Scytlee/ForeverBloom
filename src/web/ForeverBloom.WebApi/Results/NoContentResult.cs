using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace ForeverBloom.WebApi.Results;

public sealed class NoContentResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    public int StatusCode => StatusCodes.Status204NoContent;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal NoContentResult()
    {
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent));
    }
}
