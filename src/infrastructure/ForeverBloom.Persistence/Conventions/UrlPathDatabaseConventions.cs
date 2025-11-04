using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class UrlPathDatabaseConventions
{
    public static ModelConfigurationBuilder AddUrlPathConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<UrlPath>()
            .HaveMaxLength(UrlPath.MaxLength)
            .HaveConversion<UrlPathValueConverter>();

        return configuration;
    }
}

internal sealed class UrlPathValueConverter : ValueConverter<UrlPath, string>
{
    public UrlPathValueConverter()
        : base(
            urlPath => urlPath.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static UrlPath ConvertFromDatabase(string value)
    {
        var result = UrlPath.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(UrlPath),
            invalidValue: value,
            result: result);
    }
}
