using System.ComponentModel;

namespace ForeverBloom.Application.Sorting;

public enum SortDirection
{
    Ascending,
    Descending
}

public static class SortDirectionExtensions
{
    public static string ToSqlKeyword(this SortDirection direction) => direction switch
    {
        SortDirection.Ascending => "ASC",
        SortDirection.Descending => "DESC",
        _ => throw new InvalidEnumArgumentException(
            nameof(direction),
            (int)direction,
            typeof(SortDirection))
    };
}
