using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ForeverBloom.Testing.Common.BaseTestClasses;

public abstract class DatabaseTestClassBase : TestClassBase, IAsyncLifetime
{
    private readonly DatabaseFixture _postgres;
    private ApplicationDbContext _dbContext = null!;

    private string? _databaseName;
    private string? _connectionString;
    private bool _gateAcquired;

    protected DatabaseTestClassBase(DatabaseFixture postgres)
    {
        _postgres = postgres;
    }

    protected string DatabaseConnectionString => _connectionString ?? throw new InvalidOperationException("The test database connection string is not available before initialization.");
    protected ApplicationDbContext DbContext => _dbContext ?? throw new InvalidOperationException("The test DbContext is not available before initialization.");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await TestInfrastructureConcurrencyGate.WaitAsync(cancellationToken);
        _gateAcquired = true;

        _databaseName = await _postgres.CreateTestDatabaseFromTemplateAsync(_testId, cancellationToken);
        _connectionString = _postgres.BuildTestDbConnectionString(_databaseName);

        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseNpgsql(_connectionString)
          .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions, TimeProvider.System);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _dbContext.DisposeAsync();

            if (_databaseName is not null)
            {
                await _postgres.DropTestDatabaseAsync(_databaseName);
            }
        }
        finally
        {
            if (_gateAcquired)
            {
                TestInfrastructureConcurrencyGate.Release();
                _gateAcquired = false;
            }

            _dbContext = null!;
            _connectionString = null;
            _databaseName = null;

            GC.SuppressFinalize(this);
        }
    }
}
