using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class ProductNameDatabaseConventions
{
    public static ModelConfigurationBuilder AddProductNameConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<ProductName>()
            .HaveMaxLength(ProductName.MaxLength)
            .HaveConversion<ProductNameValueConverter>();

        return configuration;
    }
}

internal sealed class ProductNameValueConverter : ValueConverter<ProductName, string>
{
    public ProductNameValueConverter()
        : base(
            productName => productName.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static ProductName ConvertFromDatabase(string value)
    {
        var result = ProductName.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(ProductName),
            invalidValue: value,
            result: result);
    }
}
