using ForeverBloom.Domain.Shared;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class MetaDescriptionDatabaseConventions
{
    public static ModelConfigurationBuilder AddMetaDescriptionConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<MetaDescription>()
            .HaveMaxLength(MetaDescription.MaxLength)
            .HaveConversion<MetaDescriptionValueConverter>();

        return configuration;
    }
}

internal sealed class MetaDescriptionValueConverter : ValueConverter<MetaDescription, string>
{
    public MetaDescriptionValueConverter()
        : base(
            metaDescription => metaDescription.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static MetaDescription ConvertFromDatabase(string value)
    {
        var result = MetaDescription.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(MetaDescription),
            invalidValue: value,
            result: result);
    }
}
