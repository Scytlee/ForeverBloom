using ForeverBloom.Domain.Shared;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class SeoTitleDatabaseConventions
{
    public static ModelConfigurationBuilder AddSeoTitleConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<SeoTitle>()
            .HaveMaxLength(SeoTitle.MaxLength)
            .HaveConversion<SeoTitleValueConverter>();

        return configuration;
    }
}

internal sealed class SeoTitleValueConverter : ValueConverter<SeoTitle, string>
{
    public SeoTitleValueConverter()
        : base(
            seoTitle => seoTitle.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static SeoTitle ConvertFromDatabase(string value)
    {
        var result = SeoTitle.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(SeoTitle),
            invalidValue: value,
            result: result);
    }
}
