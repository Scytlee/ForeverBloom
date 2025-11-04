using System.Text.RegularExpressions;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Shared;

/// <summary>
/// Represents a URL-friendly slug value object.
/// </summary>
public sealed partial record Slug
{
    public string Value { get; }

    public const int MaxLength = 255;
    public const string ValidFormatPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    [GeneratedRegex(ValidFormatPattern)]
    private static partial Regex ValidFormatRegex();

    private Slug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Slug instance with domain validation.
    /// </summary>
    /// <param name="value">The slug value to validate and create.</param>
    /// <returns>A Result containing either a valid Slug or validation errors.</returns>
    public static Result<Slug> Create(string value)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new SlugErrors.Empty());
        }
        else
        {
            if (value.Length > MaxLength)
            {
                errors.Add(new SlugErrors.TooLong(value));
            }

            if (!ValidFormatRegex().IsMatch(value))
            {
                errors.Add(new SlugErrors.InvalidFormat(value));
            }
        }

        return Result<Slug>.FromValidation(errors, () => new Slug(value));
    }

    public static implicit operator string(Slug slug) => slug.Value;

}

public static class SlugErrors
{
    public sealed record Empty : IError
    {
        public string Code => "Slug.Empty";
        public string Message => "Slug cannot be empty";
    }

    public sealed record TooLong(string AttemptedSlug) : IError
    {
        public string Code => "Slug.TooLong";
        public string Message => $"Slug cannot exceed {MaxLength} characters";
        public int MaxLength => Slug.MaxLength;
    }

    public sealed record InvalidFormat(string AttemptedSlug) : IError
    {
        public string Code => "Slug.InvalidFormat";
        public string Message => $"Slug '{AttemptedSlug}' must contain only lowercase letters, numbers, and hyphens";
    }
}
