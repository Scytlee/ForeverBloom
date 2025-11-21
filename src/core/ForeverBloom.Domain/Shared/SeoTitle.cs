using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Shared;

/// <summary>
/// Value object representing an SEO-optimized title tag.
/// </summary>
public sealed record SeoTitle
{
    public const int MaxLength = 40;

    public string Value { get; }

    private SeoTitle(string value) => Value = value;

    /// <summary>
    /// Creates a new SeoTitle with validation.
    /// </summary>
    public static Result<SeoTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<SeoTitle>.Failure(new SeoTitleErrors.Empty());
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Result<SeoTitle>.Failure(new SeoTitleErrors.TooLong(trimmed));
        }

        return Result<SeoTitle>.Success(new SeoTitle(trimmed));
    }

    public override string ToString() => Value;
}

public static class SeoTitleErrors
{
    public sealed record Empty : DomainError
    {
        public override string Code => "SeoTitle.Empty";
        public override string Message => "SEO title cannot be empty";
    }

    public sealed record TooLong([AttemptedValue] string Value) : DomainError
    {
        public override string Code => "SeoTitle.TooLong";
        public override string Message => $"SEO title must be at most {MaxLength} characters, but '{Value}' has {Value.Length} characters";
        public static int MaxLength => SeoTitle.MaxLength;
    }
}
