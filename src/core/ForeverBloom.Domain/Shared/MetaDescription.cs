using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Shared;

/// <summary>
/// Value object representing an SEO meta description.
/// </summary>
public sealed record MetaDescription
{
    public const int MaxLength = 150;

    public string Value { get; }

    private MetaDescription(string value) => Value = value;

    /// <summary>
    /// Creates a new MetaDescription with validation.
    /// </summary>
    public static Result<MetaDescription> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<MetaDescription>.Failure(new MetaDescriptionErrors.Empty());
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Result<MetaDescription>.Failure(new MetaDescriptionErrors.TooLong(trimmed));
        }

        return Result<MetaDescription>.Success(new MetaDescription(trimmed));
    }

    public override string ToString() => Value;
}

public static class MetaDescriptionErrors
{
    public sealed record Empty : IError
    {
        public string Code => "MetaDescription.Empty";
        public string Message => "Meta description cannot be empty";
    }

    public sealed record TooLong(string AttemptedValue) : IError
    {
        public string Code => "MetaDescription.TooLong";
        public string Message => $"Meta description cannot exceed {MaxLength} characters";
        public int MaxLength => MetaDescription.MaxLength;
    }
}
