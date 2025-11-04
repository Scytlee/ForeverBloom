using ForeverBloom.Application.Abstractions.SlugRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ForeverBloom.Persistence.Conventions;

internal static class EntityTypeDatabaseConventions
{
    public static ModelBuilder ApplyEntityTypeCheckConstraints(this ModelBuilder builder)
    {
        var allowedCodes = Enum.GetValues<EntityType>()
            .Select(static value => (int)value)
            .ToArray();

        var allowedCodesCsv = string.Join(", ", allowedCodes);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var schema = entityType.GetSchema();
            var store = StoreObjectIdentifier.Table(tableName, schema);

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(EntityType)))
            {
                var columnName = property.GetColumnName(store)!;
                var constraintName = $"ck_{tableName}_{columnName}_valid_codes";
                var sql = $"\"{columnName}\" IN ({allowedCodesCsv})";

                builder.Entity(entityType.ClrType)
                    .ToTable(t => t.HasCheckConstraint(constraintName, sql));
            }
        }

        return builder;
    }
}
