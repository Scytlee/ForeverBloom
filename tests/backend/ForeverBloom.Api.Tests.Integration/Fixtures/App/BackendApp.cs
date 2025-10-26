using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForeverBloom.Api.Tests.Fixtures.App;

public sealed class BackendApp : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly BackendAppConfiguration _configuration;

    private BackendApp(WebApplicationFactory<Program> factory, BackendAppConfiguration configuration)
    {
        _factory = factory;
        _configuration = configuration;
    }

    internal static BackendApp Create(BackendAppConfiguration configuration)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = configuration.PostgresConnectionString
                });
            });
        });

        var app = new BackendApp(factory, configuration);

        return app;
    }

    public HttpClient RequestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return client;
    }

    public IServiceScope CreateServiceScope()
    {
        return _factory.Services.CreateScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
