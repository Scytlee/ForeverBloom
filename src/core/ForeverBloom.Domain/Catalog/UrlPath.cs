using ForeverBloom.Domain.Abstractions.Errors;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents a URL path value object.
/// </summary>
public record UrlPath
{
    public string Value { get; }

    public const int MaxLength = 500;

    private UrlPath(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new UrlPath instance with domain validation.
    /// </summary>
    /// <param name="value">The URL path to validate and create.</param>
    /// <returns>A Result containing either a valid UrlPath or validation errors.</returns>
    public static Result<UrlPath> Create(string value)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new UrlPathErrors.Empty());
        }
        else
        {
            if (value.Length > MaxLength)
            {
                errors.Add(new UrlPathErrors.TooLong(value));
            }

            if (!Uri.TryCreate(value, UriKind.Relative, out _))
            {
                errors.Add(new UrlPathErrors.InvalidFormat(value));
            }
        }

        return Result<UrlPath>.FromValidation(errors, () => new UrlPath(value));
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(UrlPath urlPath) => urlPath.Value;
}

public static class UrlPathErrors
{
    public sealed record Empty : DomainError
    {
        public override string Code => "UrlPath.Empty";
        public override string Message => "URL path cannot be empty";
    }

    public sealed record TooLong([AttemptedValue] string Path) : DomainError
    {
        public override string Code => "UrlPath.TooLong";
        public override string Message => $"URL path must be at most {MaxLength} characters, but '{Path}' has {Path.Length} characters";
        public static int MaxLength => UrlPath.MaxLength;
    }

    public sealed record InvalidFormat([AttemptedValue] string Path) : DomainError
    {
        public override string Code => "UrlPath.InvalidFormat";
        public override string Message => $"URL path '{Path}' is not a valid relative URL";
    }
}
