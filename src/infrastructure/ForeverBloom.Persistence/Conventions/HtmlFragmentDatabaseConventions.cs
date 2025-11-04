using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForeverBloom.Persistence.Conventions;

internal static class HtmlFragmentDatabaseConventions
{
    public static ModelConfigurationBuilder AddHtmlFragmentConventions(
        this ModelConfigurationBuilder configuration)
    {
        configuration.Properties<HtmlFragment>()
            .HaveMaxLength(HtmlFragment.MaxLength)
            .HaveConversion<HtmlFragmentValueConverter>();

        return configuration;
    }
}

internal sealed class HtmlFragmentValueConverter : ValueConverter<HtmlFragment, string>
{
    public HtmlFragmentValueConverter()
        : base(
            htmlFragment => htmlFragment.Value,
            value => ConvertFromDatabase(value))
    {
    }

    private static HtmlFragment ConvertFromDatabase(string value)
    {
        var result = HtmlFragment.Create(value);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new DatabaseIntegrityException(
            entityType: typeof(HtmlFragment),
            invalidValue: value,
            result: result);
    }
}
