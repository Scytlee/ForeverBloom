namespace ForeverBloom.Frontend.RazorPages.Helpers;

public static class StringHelper
{
    public static string Truncate(this string? value, int maxLength, string truncationSuffix = "...")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        var effectiveMaxLength = maxLength - truncationSuffix.Length;
        return value[..effectiveMaxLength] + truncationSuffix;
    }
}
