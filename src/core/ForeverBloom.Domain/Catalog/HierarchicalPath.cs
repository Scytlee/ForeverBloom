using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents a hierarchical path composed of dot-delimited slugs, equivalent to PostgreSQL's ltree.
/// </summary>
public sealed record HierarchicalPath
{
    public const int MaxDepth = 10;
    public const char Separator = '.';

    public string Value { get; }
    public int Depth { get; }

    private IReadOnlyCollection<string> Segments { get; }

    private HierarchicalPath(string value, params IEnumerable<string> segments)
    {
        Segments = segments.ToArray();
        Value = value;
        Depth = Segments.Count;
    }

    private HierarchicalPath(params IEnumerable<string> segments)
    {
        Segments = segments.ToArray();
        Value = string.Join(Separator, Segments);
        Depth = Segments.Count;
    }

    /// <summary>
    /// Creates a new HierarchicalPath instance with domain validation.
    /// </summary>
    /// <param name="value">The hierarchical path to validate and create.</param>
    /// <returns>A Result containing either a valid HierarchicalPath or validation errors.</returns>
    public static Result<HierarchicalPath> FromString(string value)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<HierarchicalPath>.Failure(new HierarchicalPathErrors.Empty());
        }

        // Split by separator without filtering
        var segments = value.Split(Separator);

        // Check depth
        if (segments.Length > MaxDepth)
        {
            errors.Add(new HierarchicalPathErrors.TooDeep(segments.Length));
        }

        // Validate each segment using Slug validation
        for (var i = 0; i < segments.Length; i++)
        {
            var segmentResult = Slug.Create(segments[i]);
            if (segmentResult.IsFailure)
            {
                errors.Add(new HierarchicalPathErrors.InvalidSegment(i, segments[i]));
            }
        }

        return Result<HierarchicalPath>.FromValidation(errors, () => new HierarchicalPath(value, segments));
    }

    /// <summary>
    /// Creates a child hierarchical path by appending the provided slug to the parent's path.
    /// </summary>
    /// <param name="parent">The existing parent hierarchical path.</param>
    /// <param name="childSlug">The slug for the child node.</param>
    /// <returns>A Result containing either the combined HierarchicalPath or validation errors.</returns>
    public static Result<HierarchicalPath> FromParent(HierarchicalPath parent, Slug childSlug)
    {
        // Early depth validation
        var newDepth = parent.Depth + 1;
        if (newDepth > MaxDepth)
        {
            return Result<HierarchicalPath>.Failure(new HierarchicalPathErrors.TooDeep(newDepth));
        }

        return Result<HierarchicalPath>.Success(new HierarchicalPath(parent.Segments.Append(childSlug.Value)));
    }

    /// <summary>
    /// Creates a new path with the specified slug as the last segment.
    /// The parent portion of the path remains unchanged.
    /// </summary>
    /// <param name="newSlug">The slug for the last segment.</param>
    /// <returns>A Result containing the new path with the specified slug.</returns>
    public Result<HierarchicalPath> WithSlug(Slug newSlug)
    {
        var newSegments =
            Depth == 1 ? [newSlug.Value]
            : Segments.Take(Depth - 1).Append(newSlug.Value);

        return Result<HierarchicalPath>.Success(new HierarchicalPath(newSegments));
    }

    /// <summary>
    /// Determines whether this path is a descendant of another path.
    /// Uses segment boundary validation to prevent false positives (e.g., "electronics.co" is NOT a descendant of "electronics.computers").
    /// </summary>
    /// <param name="other">The potential ancestor path.</param>
    /// <param name="includeSelf">Whether to return true if the paths are identical.</param>
    /// <returns>True if this path is a descendant of the other path; otherwise false.</returns>
    public bool IsDescendantOf(HierarchicalPath other, bool includeSelf = false)
    {
        if (Value == other.Value)
        {
            return includeSelf;
        }

        // A descendant must be deeper than its ancestor
        if (Depth <= other.Depth)
        {
            return false;
        }

        // Check if other is a prefix at a segment boundary
        // Must match exactly at segment boundaries (with trailing separator)
        return Value.StartsWith(other.Value + Separator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Rebases a path by replacing an old base prefix with a new base prefix at segment boundaries.
    /// The relative "tail" portion of the path is preserved.
    /// </summary>
    /// <param name="path">The path to rebase.</param>
    /// <param name="oldBase">The old base prefix to replace.</param>
    /// <param name="newBase">The new base prefix.</param>
    /// <returns>A Result containing the rebased path or an error.</returns>
    public static Result<HierarchicalPath> Rebase(HierarchicalPath path, HierarchicalPath oldBase, HierarchicalPath newBase)
    {
        // No-op if bases are identical
        if (oldBase.Value == newBase.Value)
        {
            return Result<HierarchicalPath>.Success(path);
        }

        // Validate that oldBase is actually a prefix of path
        if (!path.IsDescendantOf(oldBase, includeSelf: true))
        {
            return Result<HierarchicalPath>.Failure(
                new HierarchicalPathErrors.OldBaseNotAncestor(path.Value, oldBase.Value));
        }

        // Handle the case where path equals oldBase (no tail)
        if (path.Value == oldBase.Value)
        {
            return Result<HierarchicalPath>.Success(newBase);
        }

        // Extract tail segments (segments after oldBase)
        var tailSegments = path.Segments.Skip(oldBase.Depth).ToArray();

        // Combine newBase segments with tail segments
        var rebasedSegments = newBase.Segments.Concat(tailSegments);

        // Validate depth constraints
        var newDepth = newBase.Depth + tailSegments.Length;
        if (newDepth > MaxDepth)
        {
            return Result<HierarchicalPath>.Failure(
                new HierarchicalPathErrors.TooDeep(newDepth));
        }

        return Result<HierarchicalPath>.Success(new HierarchicalPath(rebasedSegments));
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(HierarchicalPath path) => path.Value;
}

public static class HierarchicalPathErrors
{
    public sealed record Empty : IError
    {
        public string Code => "HierarchicalPath.Empty";
        public string Message => "Hierarchical path cannot be empty";
    }

    public sealed record TooDeep(int ActualDepth) : IError
    {
        public string Code => "HierarchicalPath.TooDeep";
        public string Message => $"Hierarchical path depth of {ActualDepth} exceeds maximum depth of {MaxDepth}";
        public static int MaxDepth => HierarchicalPath.MaxDepth;
    }

    public sealed record InvalidSegment(int SegmentIndex, string SegmentValue) : IError
    {
        public string Code => "HierarchicalPath.InvalidSegment";
        public string Message => $"Segment at index {SegmentIndex} ('{SegmentValue}') is invalid";
    }

    public sealed record OldBaseNotAncestor(string PathValue, string OldBaseValue) : IError
    {
        public string Code => "HierarchicalPath.OldBaseNotAncestor";
        public string Message => $"Cannot rebase path '{PathValue}' because '{OldBaseValue}' is not an ancestor of the path";
    }
}
