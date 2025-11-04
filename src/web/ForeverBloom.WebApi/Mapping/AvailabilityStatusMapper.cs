using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.WebApi.Mapping;

/// <summary>
/// Maps between snake_case string representations and ProductAvailabilityStatus domain objects.
/// </summary>
public static class AvailabilityStatusMapper
{
    private static readonly Dictionary<string, ProductAvailabilityStatus> StringToStatus = new()
    {
        ["available"] = ProductAvailabilityStatus.Available,
        ["out_of_stock"] = ProductAvailabilityStatus.OutOfStock,
        ["made_to_order"] = ProductAvailabilityStatus.MadeToOrder,
        ["discontinued"] = ProductAvailabilityStatus.Discontinued,
        ["coming_soon"] = ProductAvailabilityStatus.ComingSoon
    };

    private static readonly Dictionary<ProductAvailabilityStatus, string> StatusToString = new()
    {
        [ProductAvailabilityStatus.Available] = "available",
        [ProductAvailabilityStatus.OutOfStock] = "out_of_stock",
        [ProductAvailabilityStatus.MadeToOrder] = "made_to_order",
        [ProductAvailabilityStatus.Discontinued] = "discontinued",
        [ProductAvailabilityStatus.ComingSoon] = "coming_soon"
    };

    /// <summary>
    /// Attempts to parse a snake_case string into a ProductAvailabilityStatus.
    /// </summary>
    /// <param name="value">The snake_case string value (e.g., "available", "out_of_stock").</param>
    /// <param name="status">The corresponding ProductAvailabilityStatus if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string value, out ProductAvailabilityStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return false;
        }

        return StringToStatus.TryGetValue(value.ToLowerInvariant(), out status);
    }

    /// <summary>
    /// Gets all valid snake_case string values.
    /// </summary>
    public static IReadOnlyCollection<string> ValidValues { get; } = StringToStatus.Keys.ToArray();

    /// <summary>
    /// Converts a ProductAvailabilityStatus into its snake_case string representation for API responses.
    /// </summary>
    public static string ToString(ProductAvailabilityStatus status)
    {
        if (StatusToString.TryGetValue(status, out var value))
        {
            return value;
        }

        throw new ArgumentOutOfRangeException(nameof(status), status.Code, "Unsupported availability status");
    }

    /// <summary>
    /// Converts an availability status code into its string representation.
    /// </summary>
    public static string ToString(int code)
    {
        var statusResult = ProductAvailabilityStatus.FromCode(code);
        if (statusResult.IsFailure)
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "Invalid availability status code");
        }

        return ToString(statusResult.Value);
    }
}
