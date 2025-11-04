using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace ForeverBloom.WebApi.Results;

public sealed class PermanentRedirectResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    private readonly string _location;

    public int StatusCode => StatusCodes.Status301MovedPermanently;
    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    internal PermanentRedirectResult([StringSyntax(StringSyntaxAttribute.Uri)] string location)
    {
        _location = location;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode;
        httpContext.Response.Headers.Location = _location;

        return Task.CompletedTask;
    }

    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status301MovedPermanently));
    }
}
