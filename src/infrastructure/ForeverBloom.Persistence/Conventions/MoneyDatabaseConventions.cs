using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class MoneyDatabaseConventions
{
    private const int Precision = 12;

    public static ModelConfigurationBuilder AddMoneyConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<Money>()
            .HavePrecision(Precision, Money.RequiredDecimalPlaces)
            .HaveConversion<MoneyValueConverter>();

        return configuration;
    }

    public static ModelBuilder ApplyMoneyCheckConstraints(this ModelBuilder builder)
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

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(Money)))
            {
                var columnName = property.GetColumnName(store)!;
                var constraintName = $"ck_{tableName}_{columnName}_positive";
                var sql = $"\"{columnName}\" > 0";

                builder.Entity(entityType.ClrType)
                    .ToTable(t => t.HasCheckConstraint(constraintName, sql));
            }
        }

        return builder;
    }
}

internal sealed class MoneyValueConverter : ValueConverter<Money, decimal>
{
    public MoneyValueConverter()
        : base(
            money => money.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static Money ConvertFromDatabase(decimal value)
    {
        var result = Money.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(Money),
            invalidValue: value,
            result: result);
    }
}
