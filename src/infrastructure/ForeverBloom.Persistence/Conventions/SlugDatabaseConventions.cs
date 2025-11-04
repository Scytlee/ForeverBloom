using ForeverBloom.Domain.Shared;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class SlugDatabaseConventions
{
    /// <summary>
    /// Conventions for the Slug value object.
    /// </summary>
    public static ModelConfigurationBuilder AddSlugConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<Slug>()
            .HaveMaxLength(Slug.MaxLength)
            .HaveConversion<SlugValueConverter>();

        return configuration;
    }

    /// <summary>
    /// Adds table check constraints for all Slug columns.
    /// </summary>
    public static ModelBuilder ApplySlugCheckConstraints(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue; // skip unmapped
            }

            var schema = entityType.GetSchema();
            var store = StoreObjectIdentifier.Table(tableName, schema);

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(Slug)))
            {
                var columnName = property.GetColumnName(store)!; // resolves per table mapping
                var constraintName = $"ck_{tableName}_{columnName}_format";
                var sql = $"\"{columnName}\" ~ '{Slug.ValidFormatPattern}'";

                builder.Entity(entityType.ClrType)
                    .ToTable(t => t.HasCheckConstraint(constraintName, sql));
            }
        }

        return builder;
    }
}

/// <summary>
/// EF Core value converter for Slug value objects.
/// Converts between Slug domain objects and string database representation.
/// </summary>
/// <remarks>
/// This converter enforces domain invariants during database reads.
/// If invalid data is encountered, it throws DatabaseIntegrityException to indicate
/// data corruption that requires immediate investigation.
/// </remarks>
internal sealed class SlugValueConverter : ValueConverter<Slug, string>
{
    public SlugValueConverter()
        : base(
            slug => ConvertToDatabase(slug),
            value => ConvertFromDatabase(value))
    {
    }

    private static string ConvertToDatabase(Slug slug)
    {
        return slug.Value;
    }

    private static Slug ConvertFromDatabase(string value)
    {
        var result = Slug.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(Slug),
            invalidValue: value,
            result: result);
    }
}
