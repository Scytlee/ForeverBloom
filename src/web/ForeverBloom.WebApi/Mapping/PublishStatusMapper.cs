using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Domain.Catalog;

namespace ForeverBloom.WebApi.Mapping;

/// <summary>
/// Maps between snake_case strings and publish status domain objects.
/// </summary>
public static class PublishStatusMapper
{
    private static readonly Dictionary<string, PublishStatus> StringToStatus = new()
    {
        ["draft"] = PublishStatus.Draft,
        ["published"] = PublishStatus.Published,
        ["hidden"] = PublishStatus.Hidden
    };

    private static readonly Dictionary<PublishStatus, string> StatusToString = new()
    {
        [PublishStatus.Draft] = "draft",
        [PublishStatus.Published] = "published",
        [PublishStatus.Hidden] = "hidden"
    };

    /// <summary>
    /// Attempts to parse a snake_case string into a PublishStatus.
    /// </summary>
    /// <param name="value">The snake_case string value (e.g., "draft", "published", "hidden").</param>
    /// <param name="status">The corresponding PublishStatus if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string value, [NotNullWhen(true)] out PublishStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return false;
        }

        return StringToStatus.TryGetValue(value.ToLowerInvariant(), out status);
    }

    /// <summary>
    /// Gets the valid publish status string representations.
    /// </summary>
    public static IReadOnlyCollection<string> ValidValues { get; } = StringToStatus.Keys.ToArray();

    /// <summary>
    /// Converts a publish status value object into its string representation.
    /// </summary>
    public static string ToString(PublishStatus status)
    {
        if (StatusToString.TryGetValue(status, out var value))
        {
            return value;
        }

        throw new ArgumentOutOfRangeException(nameof(status), status.Code, "Unsupported publish status");
    }

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

        return ToString(statusResult.Value);
    }
}
