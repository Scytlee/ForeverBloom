using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Domain.Abstractions;
using ForeverBloom.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace ForeverBloom.Persistence.Context;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    protected ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        base.ConfigureConventions(configuration);

        configuration.Properties<DateTimeOffset>()
          .HaveColumnType("timestamp with time zone")
          .HavePrecision(6);

        // Value object conventions
        configuration.AddSlugConventions();
        configuration.AddProductNameConventions();
        configuration.AddSeoTitleConventions();
        configuration.AddMetaDescriptionConventions();
        configuration.AddHtmlFragmentConventions();
        configuration.AddUrlPathConventions();
        configuration.AddHierarchicalPathConventions();
        configuration.AddMoneyConventions();
        configuration.AddPublishStatusConventions();
        configuration.AddProductAvailabilityStatusConventions();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Entity base class setup
        var entityTypes = builder.Model.GetEntityTypes()
          .Where(et => et.ClrType.IsAssignableTo(typeof(Entity)));

        foreach (var entityType in entityTypes)
        {
            var entityBuilder = builder.Entity(entityType.ClrType);

            entityBuilder.HasKey(nameof(Entity.Id));
            entityBuilder.Property(nameof(Entity.Id)).ValueGeneratedOnAdd();

            entityBuilder.Property(nameof(Entity.CreatedAt)).IsRequired();
            entityBuilder.Property(nameof(Entity.UpdatedAt)).IsRequired();

            entityBuilder.Property(nameof(Entity.RowVersion))
              .IsRowVersion();
        }

        // ISoftDeleteable setup
        var softDeleteableEntityTypes = builder.Model.GetEntityTypes()
          .Where(et => typeof(ISoftDeleteable).IsAssignableFrom(et.ClrType));

        foreach (var entityType in softDeleteableEntityTypes)
        {
            // Build and apply query filter to exclude deleted entities
            var parameter = Expression.Parameter(entityType.ClrType, "e"); // e
            var property = Expression.Property(parameter, nameof(ISoftDeleteable.DeletedAt)); // e.DeletedAt
            var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTimeOffset?))); // e.DeletedAt == null
            var lambda = Expression.Lambda(condition, parameter); // e => e.DeletedAt == null

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

        builder.ApplySlugCheckConstraints();
        builder.ApplyHierarchicalPathCheckConstraintsAndIndexes();
        builder.ApplyMoneyCheckConstraints();
        builder.ApplyPublishStatusCheckConstraints();
        builder.ApplyProductAvailabilityStatusCheckConstraints();
        builder.ApplyEntityTypeCheckConstraints();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateUtcTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.")]
    public async Task<T> ExecuteTransactionalAsync<T>(
        Func<CancellationToken, Task<T>> work,
        Func<T, bool>? commitCondition = null,
        IsolationLevel isolation = IsolationLevel.ReadCommitted,
        TimeSpan? lockTimeout = null,
        TimeSpan? statementTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(isolation, cancellationToken);

            // Lock timeout controls how long to wait for locks before aborting
            if (lockTimeout is not null && lockTimeout.Value > TimeSpan.Zero)
            {
                var milliseconds = (int)lockTimeout.Value.TotalMilliseconds;
                await Database.ExecuteSqlRawAsync(
                    $"SET LOCAL lock_timeout = '{milliseconds}ms';",
                    cancellationToken);
            }
            // Statement timeout caps total statement runtime
            if (statementTimeout is not null && statementTimeout.Value > TimeSpan.Zero)
            {
                var milliseconds = (int)statementTimeout.Value.TotalMilliseconds;
                await Database.ExecuteSqlRawAsync(
                    $"SET LOCAL statement_timeout = '{milliseconds}ms';",
                    cancellationToken);
            }

            try
            {
                var result = await work(cancellationToken);

                if (commitCondition is not null && !commitCondition(result))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return result;
                }

                if (ChangeTracker.HasChanges())
                {
                    await base.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private void ValidateUtcTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is Entity))
        {
            EnsureUtc(entry, nameof(Entity.CreatedAt));
            EnsureUtc(entry, nameof(Entity.UpdatedAt));

            if (entry.Entity is ISoftDeleteable)
            {
                EnsureUtc(entry, nameof(ISoftDeleteable.DeletedAt));
            }
        }

        static void EnsureUtc(EntityEntry entry, string propertyName)
        {
            var property = entry.Property(propertyName);
            if (property.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                  $"{entry.Entity.GetType().Name}.{propertyName} must be set with a UTC DateTimeOffset (offset zero).");
            }
        }
    }

}

internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql().UseSnakeCaseNamingConvention();
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
