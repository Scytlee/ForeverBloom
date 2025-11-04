using System.Reflection;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Data;
using ForeverBloom.Persistence.Repositories;
using ForeverBloom.Persistence.SlugRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ForeverBloom.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Assembly marker for reflection-based registrations.
    /// </summary>
    public static Assembly PersistenceAssembly { get; } = typeof(DependencyInjection).Assembly;

    /// <summary>
    /// Registers only the ApplicationDbContext with PostgreSQL and snake_case naming convention.
    /// Use this for tools that only need database access without the full persistence layer.
    /// </summary>
    public static IServiceCollection AddApplicationDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
                                       ?? throw new InvalidOperationException("Connection string 'Postgres' not found during DbContext registration.");

        services.AddDbContext<ApplicationDbContext>((_, options) =>
        {
            options.UseNpgsql(postgresConnectionString, npgsqlOptions =>
            {
                // Enable retry on failure for transient errors
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: [PostgresErrorCodes.SerializationFailure, PostgresErrorCodes.DeadlockDetected]);
            }).UseSnakeCaseNamingConvention();
        });

        return services;
    }

    /// <summary>
    /// Registers Persistence layer services including DbContext, UnitOfWork, and repositories.
    /// Use this for applications that need the full persistence layer.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with PostgreSQL
        services.AddApplicationDbContext(configuration);

        // Register database connection factory for Dapper queries
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

        // Register repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Register services
        services.AddScoped<ISlugRegistrationService, SlugRegistrationService>();

        // Configure Dapper to correctly map column names with underscores
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        return services;
    }
}
