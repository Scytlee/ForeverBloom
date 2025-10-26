using System.Linq.Expressions;
using ForeverBloom.Api.Models;

namespace ForeverBloom.Api.Helpers;

public static class SortingHelper
{
    public static HashSet<string> CreateAllowedSortColumns(params string[] columns)
    {
        return new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, Expression<Func<T, object>>> CreatePropertyMapping<T>(
      params (string Key, Expression<Func<T, object>> PropertyAccessor)[] mappings)
    {
        return mappings.ToDictionary(
          m => m.Key,
          m => m.PropertyAccessor,
          StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryParseAndValidateSortString(string? sortString, HashSet<string> allowedColumns, out SortCriterion[] result)
    {
        result = [];

        if (string.IsNullOrWhiteSpace(sortString))
        {
            return true;
        }

        var validColumns = new List<SortCriterion>();
        var sortPairs = sortString.Split(',');

        foreach (var sortPair in sortPairs)
        {
            var trimmed = sortPair.Trim();
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length is 0 or > 2)
            {
                return false;
            }

            var propertyName = parts[0];
            var direction = parts.Length == 2 ? parts[1].ToLowerInvariant() : "asc";

            if (direction != "asc" && direction != "desc")
            {
                return false;
            }

            if (!allowedColumns.TryGetValue(propertyName, out var canonicalPropertyName))
            {
                return false;
            }

            if (validColumns.Any(c => c.PropertyName == canonicalPropertyName))
            {
                return false;
            }

            validColumns.Add(new SortCriterion(canonicalPropertyName, direction));
        }

        result = validColumns.ToArray();
        return true;
    }
}
