using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.WebApi.Mapping;

/// <summary>
/// Maps between snake_case string representations and ProductAvailabilityStatus domain objects.
/// </summary>
public static class AvailabilityStatusMapper
{
    /// <summary>
    /// Converts a ProductAvailabilityStatus into its snake_case string representation.
    /// </summary>
    public static string ToString(ProductAvailabilityStatus status) => status.Name;

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

        return statusResult.Value.Name;
    }
}
