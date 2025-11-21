using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ForeverBloom.Testing.Integration.BaseTestClasses;

/// <summary>
/// Provides per-test lifetime management for <see cref="WebApiTestFixture"/> and exposes
/// helpers for both HTTP clients and direct Application-layer access.
/// </summary>
public abstract class WebApiIntegrationTestBase : TestBase, IAsyncLifetime
{
    private WebApiTestFixture? _fixture;

    protected WebApiTestFixture Fixture =>
        _fixture ?? throw new InvalidOperationException("WebApi fixture is not initialized.");

    protected ApplicationTestFixture Application => Fixture.Application;

    public async ValueTask InitializeAsync()
    {
        _fixture = new WebApiTestFixture();
        await _fixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }
    }

    protected HttpClient CreateClient(WebApiKeyScope scope = WebApiKeyScope.None)
    {
        return Fixture.CreateClient(scope);
    }

    protected IServiceScope CreateServiceScope()
    {
        return Fixture.CreateServiceScope();
    }

    protected Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        DateTimeOffset? actionTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Application.SendAsync(request, actionTimestamp, cancellationToken);
    }

    protected Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Application.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Application.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, CancellationToken, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Application.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Application.ExecuteDbContextAsync(operation, CancellationToken);
    }
}
