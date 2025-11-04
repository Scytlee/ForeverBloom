using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class ProductAvailabilityStatusDatabaseConventions
{
    public static ModelConfigurationBuilder AddProductAvailabilityStatusConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<ProductAvailabilityStatus>()
            .HaveConversion<ProductAvailabilityStatusValueConverter>();

        return configuration;
    }

    public static ModelBuilder ApplyProductAvailabilityStatusCheckConstraints(this ModelBuilder builder)
    {
        var allowedCodes = ProductAvailabilityStatus.All.Select(status => status.Code).ToArray();
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

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(ProductAvailabilityStatus)))
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

internal sealed class ProductAvailabilityStatusValueConverter : ValueConverter<ProductAvailabilityStatus, int>
{
    public ProductAvailabilityStatusValueConverter()
        : base(
            status => status.Code,
            code => ConvertFromDatabase(code))
    {
    }

    private static ProductAvailabilityStatus ConvertFromDatabase(int code)
    {
        var result = ProductAvailabilityStatus.FromCode(code);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(ProductAvailabilityStatus),
            invalidValue: code,
            result: result);
    }
}
