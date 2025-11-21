using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents a hierarchical path composed of dot-delimited slugs.
/// </summary>
public sealed record HierarchicalPath
{
    public const int MaxDepth = 10;
    public const char Separator = '.';

    public string Value
    {
        get
        {
            return field ??= string.Join(Separator, Segments.Select(s => s.Value));
        }
    }

    public int Depth { get; }

    private Slug[] Segments { get; }

    private HierarchicalPath(params Slug[] slugs)
    {
        Segments = slugs;
        Depth = Segments.Length;
    }

    /// <summary>
    /// Creates a HierarchicalPath from a sequence of slugs.
    /// </summary>
    /// <param name="slugs">The slugs representing the hierarchical path.</param>
    /// <returns>A Result containing either the HierarchicalPath or validation errors.</returns>
    public static Result<HierarchicalPath> FromSlugs(params Slug[] slugs)
    {
        var errors = new List<IError>();

        if (slugs.Length == 0)
        {
            errors.Add(new HierarchicalPathErrors.Empty());
        }

        if (slugs.Length > MaxDepth)
        {
            errors.Add(new HierarchicalPathErrors.TooDeep(slugs.Length));
        }

        return Result<HierarchicalPath>.FromValidation(
            errors,
            () => new HierarchicalPath(slugs));
    }

    /// <summary>
    /// Creates a new HierarchicalPath instance with domain validation.
    /// </summary>
    /// <param name="value">The hierarchical path to validate and create.</param>
    /// <returns>A Result containing either a valid HierarchicalPath or validation errors.</returns>
    public static Result<HierarchicalPath> FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<HierarchicalPath>.Failure(new HierarchicalPathErrors.Empty());
        }

        var errors = new List<IError>();
        var segments = value.Split(Separator);

        if (segments.Length > MaxDepth)
        {
            errors.Add(new HierarchicalPathErrors.TooDeep(segments.Length));
        }

        // Validate each segment and collect slugs
        var slugs = new List<Slug>();
        for (var i = 0; i < segments.Length; i++)
        {
            var segmentResult = Slug.Create(segments[i]);
            if (segmentResult.IsFailure)
            {
                errors.Add(new HierarchicalPathErrors.InvalidSegment(i, segments[i]));
            }
            else
            {
                slugs.Add(segmentResult.Value);
            }
        }

        return Result<HierarchicalPath>.FromValidation(
            errors,
            () => new HierarchicalPath(slugs.ToArray()));
    }

    /// <summary>
    /// Creates a child hierarchical path by appending the provided slug to the parent's path.
    /// </summary>
    /// <param name="parent">The existing parent hierarchical path.</param>
    /// <param name="childSlug">The slug for the child node.</param>
    /// <returns>A Result containing either the combined HierarchicalPath or validation errors.</returns>
    public static Result<HierarchicalPath> FromParent(HierarchicalPath parent, Slug childSlug)
    {
        var errors = new List<IError>();

        var newDepth = parent.Depth + 1;
        if (newDepth > MaxDepth)
        {
            errors.Add(new HierarchicalPathErrors.TooDeep(newDepth));
        }

        return Result<HierarchicalPath>.FromValidation(
            errors,
            () => new HierarchicalPath([.. parent.Segments, childSlug]));
    }

    /// <summary>
    /// Creates a new path with the specified slug as the last segment.
    /// The parent portion of the path remains unchanged.
    /// </summary>
    /// <param name="newSlug">The slug for the last segment.</param>
    /// <returns>A Result containing the new path with the specified slug.</returns>
    public HierarchicalPath WithSlug(Slug newSlug)
    {
        var newSegments = Depth == 1
            ? [newSlug]
            : Segments.Take(Depth - 1).Append(newSlug).ToArray();

        return new HierarchicalPath(newSegments);
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

        // Validate depth constraints
        var newDepth = newBase.Depth + tailSegments.Length;
        if (newDepth > MaxDepth)
        {
            return Result<HierarchicalPath>.Failure(
                new HierarchicalPathErrors.TooDeep(newDepth));
        }

        // Combine newBase segments with tail segments
        var rebasedSegments = newBase.Segments.Concat(tailSegments).ToArray();

        return FromSlugs(rebasedSegments);
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(HierarchicalPath path) => path.Value;

    public bool Equals(HierarchicalPath? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value);
    }

    public override int GetHashCode() => Value.GetHashCode();
}

public static class HierarchicalPathErrors
{
    public sealed record Empty : DomainError
    {
        public override string Code => "HierarchicalPath.Empty";
        public override string Message => "Hierarchical path cannot be empty";
    }

    public sealed record TooDeep([AttemptedValue] int Depth) : DomainError
    {
        public override string Code => "HierarchicalPath.TooDeep";
        public override string Message => $"Hierarchical path depth must be at most {MaxDepth}, but was {Depth}";
        public static int MaxDepth => HierarchicalPath.MaxDepth;
    }

    public sealed record InvalidSegment(int SegmentIndex, [AttemptedValue] string SegmentValue) : DomainError
    {
        public override string Code => "HierarchicalPath.InvalidSegment";
        public override string Message => $"Segment at index {SegmentIndex} ('{SegmentValue}') is invalid";
    }

    public sealed record OldBaseNotAncestor([AttemptedValue] string Path, string OldBase) : DomainError
    {
        public override string Code => "HierarchicalPath.OldBaseNotAncestor";
        public override string Message => $"Cannot rebase path '{Path}' because '{OldBase}' is not an ancestor of the path";
    }
}
