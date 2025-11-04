using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class PublishStatusDatabaseConventions
{
    public static ModelConfigurationBuilder AddPublishStatusConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<PublishStatus>()
            .HaveConversion<PublishStatusValueConverter>();

        return configuration;
    }

    public static ModelBuilder ApplyPublishStatusCheckConstraints(this ModelBuilder builder)
    {
        var allowedCodes = PublishStatus.All.Select(status => status.Code).ToArray();
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

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(PublishStatus)))
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

internal sealed class PublishStatusValueConverter : ValueConverter<PublishStatus, int>
{
    public PublishStatusValueConverter()
        : base(
            status => status.Code,
            code => ConvertFromDatabase(code))
    {
    }

    private static PublishStatus ConvertFromDatabase(int code)
    {
        var result = PublishStatus.FromCode(code);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(PublishStatus),
            invalidValue: code,
            result: result);
    }
}
