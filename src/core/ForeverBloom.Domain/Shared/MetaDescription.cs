using ForeverBloom.Domain.Abstractions.Errors;
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
    public sealed record Empty : DomainError
    {
        public override string Code => "MetaDescription.Empty";
        public override string Message => "Meta description cannot be empty";
    }

    public sealed record TooLong([AttemptedValue] string Value) : DomainError
    {
        public override string Code => "MetaDescription.TooLong";
        public override string Message => $"Meta description must be at most {MaxLength} characters, but '{Value}' has {Value.Length} characters";
        public static int MaxLength => MetaDescription.MaxLength;
    }
}
