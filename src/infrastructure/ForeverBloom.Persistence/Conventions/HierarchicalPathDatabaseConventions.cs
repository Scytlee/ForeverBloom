using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ForeverBloom.Persistence.Conventions;

internal static class HierarchicalPathDatabaseConventions
{
    public static ModelConfigurationBuilder AddHierarchicalPathConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<HierarchicalPath>()
            .HaveColumnType("ltree")
            .HaveConversion<HierarchicalPathValueConverter>();

        return configuration;
    }

    public static ModelBuilder ApplyHierarchicalPathCheckConstraintsAndIndexes(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var schema = entityType.GetSchema();
            var store = StoreObjectIdentifier.Table(tableName, schema);

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(HierarchicalPath)))
            {
                var columnName = property.GetColumnName(store)!;
                var constraintName = $"ck_{tableName}_{columnName}_segments";
                var sql = $"nlevel(\"{columnName}\") <= {HierarchicalPath.MaxDepth}";

                builder.Entity(entityType.ClrType)
                    .ToTable(t => t.HasCheckConstraint(constraintName, sql))
                    .HasIndex(property.Name)
                    .HasMethod("gist");
            }
        }

        return builder;
    }
}

internal sealed class HierarchicalPathValueConverter : ValueConverter<HierarchicalPath, LTree>
{
    public HierarchicalPathValueConverter()
        : base(
            path => new LTree(path.Value),
            value => ConvertFromDatabase(value))
    {
    }

    private static HierarchicalPath ConvertFromDatabase(LTree value)
    {
        var result = HierarchicalPath.FromString(value.ToString());

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(HierarchicalPath),
            invalidValue: value,
            result: result);
    }
}
