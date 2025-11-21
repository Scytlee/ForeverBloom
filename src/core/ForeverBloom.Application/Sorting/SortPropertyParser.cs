using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Sorting;

/// <summary>
/// Parses sort properties from comma-separated string format to SortProperty objects.
/// </summary>
public static class SortPropertyParser
{
    /// <summary>
    /// Attempts to parse a comma-separated sort string into an array of SortProperty objects.
    /// Expected format: "property:direction,property:direction" (e.g., "name:asc,price:desc")
    /// </summary>
    /// <param name="sortBy">The sort string to parse, or null/empty for no sorting.</param>
    /// <returns>A Result containing the parsed SortProperty array, or an error if parsing failed.</returns>
    public static Result<SortProperty[]> Parse(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return Result<SortProperty[]>.Success([]);
        }

        var errors = new List<IError>();
        var parsedProperties = new List<SortProperty>();

        var propertyStrings = sortBy.Split(',', StringSplitOptions.TrimEntries);

        foreach (var propertyString in propertyStrings)
        {
            var parts = propertyString.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                errors.Add(new SortingErrors.InvalidSortFormat(propertyString));
                continue;
            }

            var propertyName = parts[0];
            var directionString = parts[1];

            // Parse direction
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
                errors.Add(new SortingErrors.InvalidSortDirection(directionString));
                continue;
            }

            parsedProperties.Add(new SortProperty(propertyName, direction));
        }

        return Result<SortProperty[]>.FromValidation(errors, () => parsedProperties.ToArray());
    }
}
