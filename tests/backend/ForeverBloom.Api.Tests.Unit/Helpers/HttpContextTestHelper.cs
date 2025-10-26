using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ForeverBloom.Api.Tests.Helpers;

public static class HttpContextTestHelper
{
    public static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.Configure<JsonOptions>(_ => { });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };

        return httpContext;
    }

    public static async Task<T?> ReadResponseAsync<T>(DefaultHttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Body.Position = 0;

        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var serializerOptions = jsonOptions.SerializerOptions ?? JsonSerializerOptions.Default;

        return await JsonSerializer.DeserializeAsync<T>(httpContext.Response.Body, serializerOptions, cancellationToken);
    }
}
