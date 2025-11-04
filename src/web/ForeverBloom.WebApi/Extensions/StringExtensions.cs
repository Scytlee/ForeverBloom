namespace ForeverBloom.WebApi.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string str)
    {
        return string.IsNullOrWhiteSpace(str)
            ? str
            : $"{char.ToLowerInvariant(str[0])}{str[1..]}";
    }
}
