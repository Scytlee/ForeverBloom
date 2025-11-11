using ForeverBloom.Application;
using ForeverBloom.Persistence;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Integration.Database;
using ForeverBloom.Testing.Integration.DependencyInjection;
using ForeverBloom.Testing.Integration.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Xunit;

namespace ForeverBloom.Testing.Integration.Fixtures;

/// <summary>
/// Minimal Application host that wires up Application + Persistence to execute use cases directly.
/// </summary>
public sealed class ApplicationTestFixture : IAsyncLifetime
{
    private readonly Guid _fixtureId = Guid.NewGuid();

    private ServiceProvider? _serviceProvider;
    private TestDatabaseInstance? _databaseInstance;
    private bool _gateAcquired;

    public IServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException("Services are not available before initialization.");

    public ApplicationDbContext DbContext => Services.GetRequiredService<ApplicationDbContext>();

    public ISender Sender => Services.GetRequiredService<ISender>();

    public async ValueTask InitializeAsync()
    {
        await PostgresTemplateDatabaseLifetime.EnsureInitializedAsync(
            MigrateTemplateDatabaseAsync,
            CancellationToken.None);

        await TestInfrastructureConcurrencyGate.WaitAsync();
        _gateAcquired = true;

        try
        {
            _databaseInstance = await PostgresTemplateDatabaseLifetime.Manager.CreateDatabaseAsync(_fixtureId);
            _serviceProvider = BuildServiceProvider(_databaseInstance.ConnectionString);
        }
        catch
        {
            await CleanupAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
    }

    public Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Sender.Send(request, cancellationToken);
    }

    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        var configurationValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:Postgres"] = connectionString
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddApplication();
        services.AddTestInfrastructure();
        services.AddPersistence(configuration);

        return services.BuildServiceProvider();
    }

    private static async Task MigrateTemplateDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, ConfigureNpgsqlOptions)
            .UseSnakeCaseNamingConvention();

        await using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static void ConfigureNpgsqlOptions(NpgsqlDbContextOptionsBuilder options)
    {
        options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: Array.Empty<string>());
    }

    private async Task CleanupAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }

        if (_databaseInstance is not null)
        {
            await PostgresTemplateDatabaseLifetime.Manager.DropDatabaseAsync(_databaseInstance.Name);
            _databaseInstance = null;
        }

        if (_gateAcquired)
        {
            TestInfrastructureConcurrencyGate.Release();
            _gateAcquired = false;
        }
    }
}
