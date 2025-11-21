using ForeverBloom.Application;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Persistence;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Integration.DependencyInjection;
using ForeverBloom.Testing.Integration.Infrastructure;
using ForeverBloom.Testing.Integration.Time;
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
    internal static readonly DateTimeOffset InitialTimeSeed = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly bool _ownsEnvironment;

    private IntegrationTestEnvironment? _environment;
    private ServiceProvider? _serviceProvider;

    private IntegrationTestEnvironment Environment =>
        _environment ?? throw new InvalidOperationException("Test environment is not available.");

    public ApplicationTestFixture()
    {
        _ownsEnvironment = true;
    }

    internal ApplicationTestFixture(IntegrationTestEnvironment sharedEnvironment)
    {
        _environment = sharedEnvironment ?? throw new ArgumentNullException(nameof(sharedEnvironment));
        _ownsEnvironment = false;
    }

    public IServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException("Services are not available before initialization.");

    public TestTimeProvider TimeProvider => Environment.TimeProvider;

    public async ValueTask InitializeAsync()
    {
        if (_ownsEnvironment && _environment is null)
        {
            _environment = await IntegrationTestEnvironment.CreateAsync(
                MigrateTemplateDatabaseAsync,
                InitialTimeSeed,
                CancellationToken.None);
        }

        if (_environment is null)
        {
            throw new InvalidOperationException("Integration test environment must be provided before initialization.");
        }

        try
        {
            _serviceProvider = BuildServiceProvider(Environment);
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

    public async Task<TResult> SendAsync<TResult>(
        IRequest<TResult> request,
        DateTimeOffset? actionTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var scope = Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var autoAdvancePreAction = Environment.TimeProvider.AutoAdvance;

        try
        {
            if (actionTimestamp is not null && actionTimestamp != DateTimeOffset.MinValue)
            {
                Environment.TimeProvider.FreezeAt(actionTimestamp.Value);
            }
            return await sender.Send(request, cancellationToken);
        }
        finally
        {
            if (autoAdvancePreAction)
            {
                Environment.TimeProvider.Unfreeze();
            }
        }
    }

    public async Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await operation(dbContext, cancellationToken);
    }

    public Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteDbContextAsync((dbContext, _) => operation(dbContext), cancellationToken);
    }

    public async Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await operation(dbContext, cancellationToken);
    }

    public Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ApplicationDbContext, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteDbContextAsync((dbContext, _) => operation(dbContext), cancellationToken);
    }

    private static ServiceProvider BuildServiceProvider(IntegrationTestEnvironment environment)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(environment.ConfigurationValues)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddApplication();
        services.AddTestInfrastructure();
        services.AddSingleton<ITimeProvider>(environment.TimeProvider);
        services.AddPersistence(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    internal static async Task MigrateTemplateDatabaseAsync(string connectionString, CancellationToken cancellationToken)
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

        if (_environment is not null && _ownsEnvironment)
        {
            await _environment.DisposeAsync();
        }

        _environment = null;
    }
}
