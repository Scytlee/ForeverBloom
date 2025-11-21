using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.BaseTestClasses;
using ForeverBloom.Testing.Integration.Fixtures;
using MediatR;
using Xunit;

namespace ForeverBloom.Testing.Integration.BaseTestClasses;

/// <summary>
/// Provides per-test lifetime management for <see cref="ApplicationTestFixture"/>.
/// </summary>
public abstract class ApplicationIntegrationTestBase : TestBase, IAsyncLifetime
{
    private ApplicationTestFixture? _fixture;

    protected ApplicationTestFixture Fixture =>
        _fixture ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        _fixture = new ApplicationTestFixture();
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

    protected Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        DateTimeOffset? actionTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Fixture.SendAsync(request, actionTimestamp, cancellationToken);
    }

    protected Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Fixture.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Fixture.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, CancellationToken, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Fixture.ExecuteDbContextAsync(operation, CancellationToken);
    }

    protected Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Fixture.ExecuteDbContextAsync(operation, CancellationToken);
    }
}
