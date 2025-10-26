using System.Linq.Expressions;
using ForeverBloom.Persistence.Entities;
using ForeverBloom.Persistence.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace ForeverBloom.Persistence.Context;

public class ApplicationDbContext : DbContext
{
    private const PersistenceFeatures DefaultFeatures = PersistenceFeatures.StampAuditTimestamps;
    private readonly TimeProvider _clock = TimeProvider.System;
    private PersistenceFeatures? _overrideFeatures;

    protected ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider clock)
      : base(options)
    {
        _clock = clock;
    }

    public const string BusinessSchema = "business";

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<SlugRegistryEntry> SlugRegistry { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        base.ConfigureConventions(configuration);

        configuration.Properties<DateTimeOffset>()
          .HaveColumnType("timestamp with time zone")
          .HavePrecision(6);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // IAuditable setup
        var auditableEntityTypes = builder.Model.GetEntityTypes()
          .Where(et => typeof(IAuditable).IsAssignableFrom(et.ClrType));

        foreach (var entityType in auditableEntityTypes)
        {
            var entity = builder.Entity(entityType.ClrType);

            // Configure audit property storage
            entity.Property(nameof(IAuditable.CreatedAt))
              .HasColumnType("timestamp with time zone")
              .HasPrecision(6)
              .IsRequired();

            entity.Property(nameof(IAuditable.UpdatedAt))
              .HasColumnType("timestamp with time zone")
              .HasPrecision(6)
              .IsRequired();

            // Configure the RowVersion property
            entity.Property(nameof(IAuditable.RowVersion))
              .IsRowVersion();
        }

        // ISoftDeleteable setup
        var softDeleteableEntityTypes = builder.Model.GetEntityTypes()
          .Where(et => typeof(ISoftDeleteable).IsAssignableFrom(et.ClrType));

        foreach (var entityType in softDeleteableEntityTypes)
        {
            builder.Entity(entityType.ClrType)
              .Property(nameof(ISoftDeleteable.DeletedAt))
              .HasColumnType("timestamp with time zone")
              .HasPrecision(6);

            // Build and apply query filter to exclude deleted entities
            var parameter = Expression.Parameter(entityType.ClrType, "e"); // e
            var property = Expression.Property(parameter, nameof(ISoftDeleteable.DeletedAt)); // e.DeletedAt
            var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTimeOffset?))); // e.DeletedAt == null
            var lambda = Expression.Lambda(condition, parameter); // e => e.DeletedAt == null

            builder.Entity(entityType.ClrType)
              .HasQueryFilter(lambda);
        }
    }

    public void SuppressFeatures(PersistenceFeatures featuresToSuppress)
    {
        _overrideFeatures = DefaultFeatures & ~featuresToSuppress;
    }

    public void OverrideFeatures(PersistenceFeatures newFeatureSet)
    {
        _overrideFeatures = newFeatureSet;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            ApplyPersistenceFeatures();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        finally
        {
            _overrideFeatures = null;
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyPersistenceFeatures();
            return base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _overrideFeatures = null;
        }
    }

    private void ApplyPersistenceFeatures()
    {
        var features = _overrideFeatures ?? DefaultFeatures;

        if (features.HasFlag(PersistenceFeatures.StampAuditTimestamps))
        {
            StampAuditTimestamps();
        }

        // UTC validation is always active - it prevents database errors
        ValidateUtcTimestamps();
    }

    private void StampAuditTimestamps()
    {
        var utcNow = _clock.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is IAuditable))
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    Set(entry, nameof(IAuditable.CreatedAt), utcNow);
                    Set(entry, nameof(IAuditable.UpdatedAt), utcNow);
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;

                    if (entry.Entity is ISoftDeleteable)
                    {
                        var deletedAtProperty = entry.Property(nameof(ISoftDeleteable.DeletedAt));
                        var updatedAtProperty = entry.Property(nameof(IAuditable.UpdatedAt));

                        var isDeletedAtModified = deletedAtProperty.IsModified;
                        var deletedAtValue = (DateTimeOffset?)deletedAtProperty.CurrentValue;

                        updatedAtProperty.CurrentValue = (isDeletedAtModified, deletedAtValue) switch
                        {
                            (true, null) => utcNow,
                            (true, not null) => deletedAtValue.Value.ToUniversalTime(),
                            _ => utcNow
                        };
                    }
                    else
                    {
                        Set(entry, nameof(IAuditable.UpdatedAt), utcNow);
                    }
                    break;
            }
        }

        static void Set(EntityEntry entry, string propertyName, DateTimeOffset? value)
        {
            var property = entry.Property(propertyName);
            property.CurrentValue = value?.ToUniversalTime();
        }
    }

    private void ValidateUtcTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is IAuditable))
        {
            EnsureUtc(entry, nameof(IAuditable.CreatedAt));
            EnsureUtc(entry, nameof(IAuditable.UpdatedAt));

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
        optionsBuilder.UseNpgsql();
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
