using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Integration.Fixtures;
using MediatR;
using Xunit;

namespace ForeverBloom.Testing.Integration.BaseTestClasses;

/// <summary>
/// Provides per-test lifetime management for <see cref="ApplicationTestFixture"/>.
/// </summary>
public abstract class ApplicationIntegrationTestBase : IAsyncLifetime
{
    private ApplicationTestFixture? _fixture;

    protected Guid TestId { get; } = Guid.NewGuid();

    protected ApplicationTestFixture Fixture =>
        _fixture ?? throw new InvalidOperationException("Fixture is not initialized.");

    protected ApplicationDbContext DbContext => Fixture.DbContext;

    protected ISender Sender => Fixture.Sender;

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

    protected Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Sender.Send(request, cancellationToken);
    }
}
