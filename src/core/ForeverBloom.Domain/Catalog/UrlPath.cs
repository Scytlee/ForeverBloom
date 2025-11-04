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
    public sealed record Empty : IError
    {
        public string Code => "UrlPath.Empty";
        public string Message => "URL path cannot be empty";
    }

    public sealed record TooLong(string AttemptedPath) : IError
    {
        public string Code => "UrlPath.TooLong";
        public string Message => $"URL path cannot exceed {MaxLength} characters";
        public int MaxLength => UrlPath.MaxLength;
    }

    public sealed record InvalidFormat(string AttemptedPath) : IError
    {
        public string Code => "UrlPath.InvalidFormat";
        public string Message => $"URL path '{AttemptedPath}' is not a valid relative URL";
    }
}
