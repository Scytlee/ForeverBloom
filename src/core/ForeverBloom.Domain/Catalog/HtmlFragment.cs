using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents a well-formed HTML fragment value object.
/// </summary>
public sealed record HtmlFragment
{
    public string Value { get; }

    public const int MaxLength = 4000;

    private HtmlFragment(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new HtmlFragment instance with domain validation and normalizes the HTML fragment.
    /// </summary>
    /// <param name="value">The HTML fragment to validate and normalize before creating the value object.</param>
    /// <returns>A Result containing either a valid HtmlFragment or validation errors.</returns>
    public static Result<HtmlFragment> Create(string value)
    {
        var errors = new List<IError>();

        string? normalizedValue = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new HtmlFragmentErrors.Empty());
        }
        else
        {
            if (value.Length > MaxLength)
            {
                errors.Add(new HtmlFragmentErrors.TooLong(value));
            }

            // Validate HTML structure using AngleSharp
            var context = BrowsingContext.New(Configuration.Default);
            var document = context.OpenNewAsync().GetAwaiter().GetResult();
            var container = document.CreateElement("div");

            try
            {
                var parser = new HtmlParser(new HtmlParserOptions { IsStrictMode = true }, context);
                var nodes = parser.ParseFragment(value, container);

                if (!string.IsNullOrWhiteSpace(value) && nodes.Length == 0)
                {
                    errors.Add(new HtmlFragmentErrors.Malformed(value));
                }

                normalizedValue = string.Concat(nodes.Select(n => n.ToHtml(HtmlMarkupFormatter.Instance)));
            }
            catch (Exception)
            {
                errors.Add(new HtmlFragmentErrors.Malformed(value));
            }
        }

        return Result<HtmlFragment>.FromValidation(errors, () => new HtmlFragment(normalizedValue!));
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(HtmlFragment htmlFragment) => htmlFragment.Value;
}

public static class HtmlFragmentErrors
{
    public sealed record Empty : IError
    {
        public string Code => "HtmlFragment.Empty";
        public string Message => "HTML fragment cannot be empty";
    }

    public sealed record TooLong(string AttemptedValue) : IError
    {
        public string Code => "HtmlFragment.TooLong";
        public string Message => $"HTML fragment cannot exceed {MaxLength} characters";
        public static int MaxLength => HtmlFragment.MaxLength;
    }

    public sealed record Malformed(string AttemptedValue) : IError
    {
        public string Code => "HtmlFragment.Malformed";
        public string Message => "HTML fragment is malformed or contains invalid HTML structure";
    }
}
