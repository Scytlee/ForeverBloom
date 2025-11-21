using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Testing.Integration.Infrastructure;
using ForeverBloom.Testing.Integration.Time;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ForeverBloom.Testing.Integration.Fixtures;

/// <summary>
/// Spins up the WebApi host on top of the shared integration environment
/// and exposes helpers for authenticated HttpClients and typed API clients.
/// </summary>
public sealed class WebApiTestFixture : IAsyncLifetime
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string FrontendKey = "FrontendKey";
    private const string AdminKey = "AdminKey";

    private IntegrationTestEnvironment? _environment;
    private ForeverBloomWebApiFactory? _factory;
    private ApplicationTestFixture? _applicationFixture;

    private ForeverBloomWebApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("WebApplicationFactory is not initialized.");

    private IntegrationTestEnvironment Environment =>
        _environment ?? throw new InvalidOperationException("Integration test environment is not initialized.");

    public ApplicationTestFixture Application =>
        _applicationFixture ?? throw new InvalidOperationException("Application fixture is not initialized.");

    public TestTimeProvider TimeProvider => Environment.TimeProvider;

    public async ValueTask InitializeAsync()
    {
        _environment = await IntegrationTestEnvironment.CreateAsync(
            ApplicationTestFixture.MigrateTemplateDatabaseAsync,
            ApplicationTestFixture.InitialTimeSeed,
            CancellationToken.None);

        _factory = new ForeverBloomWebApiFactory(Environment);

        _applicationFixture = new ApplicationTestFixture(Environment);
        await _applicationFixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            _factory.Dispose();
            _factory = null;
        }

        if (_applicationFixture is not null)
        {
            await _applicationFixture.DisposeAsync();
            _applicationFixture = null;
        }

        if (_environment is not null)
        {
            await _environment.DisposeAsync();
            _environment = null;
        }
    }

    public HttpClient CreateClient(WebApiKeyScope scope = WebApiKeyScope.None)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.BaseAddress = BuildApiBaseAddress(client.BaseAddress ?? new Uri("http://localhost"));

        ApplyApiKey(client, scope);
        return client;
    }

    public IServiceScope CreateServiceScope()
    {
        return Factory.Services.CreateScope();
    }

    private static Uri BuildApiBaseAddress(Uri rootAddress)
    {
        var baseUri = rootAddress;
        if (!baseUri.AbsolutePath.EndsWith("/"))
        {
            baseUri = new Uri(baseUri, baseUri.AbsolutePath + "/");
        }

        return new Uri(baseUri, "api/v1/");
    }

    private static void ApplyApiKey(HttpClient client, WebApiKeyScope scope)
    {
        client.DefaultRequestHeaders.Remove(ApiKeyHeaderName);

        var keyValue = scope switch
        {
            WebApiKeyScope.Admin => AdminKey,
            WebApiKeyScope.Frontend => FrontendKey,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(keyValue))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeaderName, keyValue);
        }
    }

    private sealed class ForeverBloomWebApiFactory : WebApplicationFactory<Program>
    {
        private readonly IntegrationTestEnvironment _environment;

        public ForeverBloomWebApiFactory(IntegrationTestEnvironment environment)
        {
            _environment = environment;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_environment.ConfigurationValues);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITimeProvider>();
                services.AddSingleton<ITimeProvider>(_environment.TimeProvider);
            });
        }
    }
}
