using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.WebApi.Mapping;

/// <summary>
/// Maps between snake_case strings and publish status domain objects.
/// </summary>
public static class PublishStatusMapper
{
    /// <summary>
    /// Converts a publish status value object into its string representation.
    /// </summary>
    public static string ToString(PublishStatus status) => status.Name;

    /// <summary>
    /// Converts a publish status code into its string representation.
    /// </summary>
    public static string ToString(int code)
    {
        var statusResult = PublishStatus.FromCode(code);
        if (statusResult.IsFailure)
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "Invalid publish status code");
        }

        return statusResult.Value.Name;
    }
}
