using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Application.Sorting;
using ForeverBloom.WebApi.Models;

namespace ForeverBloom.WebApi.Parsing;

/// <summary>
/// Parses sort properties from HTTP query string format to SortProperty objects.
/// </summary>
internal static class SortPropertyParser
{
    private static readonly string[] ValidDirections = ["asc", "desc"];

    /// <summary>
    /// Attempts to parse a comma-separated sort string into an array of SortProperty objects.
    /// Expected format: "property:direction,property:direction" (e.g., "name:asc,price:desc")
    /// </summary>
    /// <param name="sortBy">The sort string to parse, or null/empty for no sorting.</param>
    /// <param name="result">The parsed SortCriterion array (or <c>null</c>, if not provided) if successful.</param>
    /// <param name="error">The validation error if parsing failed.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(
        string? sortBy,
        out SortProperty[]? result,
        [NotNullWhen(false)] out ValidationErrorDetail? error)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            result = null;
            error = null;
            return true;
        }

        var parsedProperties = new List<SortProperty>();
        var propertyStrings = sortBy.Split(',', StringSplitOptions.TrimEntries); // empty = invalid

        foreach (var propertyString in propertyStrings)
        {
            var parts = propertyString.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                result = null;
                error = new ValidationErrorDetail(
                    code: "SortBy.InvalidFormat",
                    message: "Each sort property must be in the format 'property:direction' (e.g., 'name:asc').",
                    attemptedValue: propertyString,
                    customState: null);
                return false;
            }

            var propertyName = parts[0];
            var directionString = parts[1];

            // Validate and convert direction string
            SortDirection direction;
            if (string.Equals(directionString, "asc", StringComparison.OrdinalIgnoreCase))
            {
                direction = SortDirection.Ascending;
            }
            else if (string.Equals(directionString, "desc", StringComparison.OrdinalIgnoreCase))
            {
                direction = SortDirection.Descending;
            }
            else
            {
                result = null;
                error = new ValidationErrorDetail(
                    code: "SortBy.InvalidDirection",
                    message: "Sort direction must be either 'asc' or 'desc'.",
                    attemptedValue: directionString,
                    customState: new { ValidDirections });
                return false;
            }

            parsedProperties.Add(new SortProperty(propertyName, direction));
        }

        result = parsedProperties.ToArray();
        error = null;
        return true;
    }
}
