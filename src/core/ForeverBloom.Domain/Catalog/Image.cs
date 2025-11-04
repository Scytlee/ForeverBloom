using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Domain.Catalog;

/// <summary>
/// Represents an image with its path and optional alternative text.
/// </summary>
public sealed record Image
{
    public UrlPath Source { get; }
    public string? AltText { get; }

    public const int AltTextMaxLength = 200;
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"];

    private Image(UrlPath source, string? altText)
    {
        Source = source;
        AltText = altText;
    }

    /// <summary>
    /// Creates a new Image instance with domain validation.
    /// </summary>
    /// <param name="sourcePath">The URL path for the image.</param>
    /// <param name="altText">The optional alt text.</param>
    /// <returns>A Result containing either a valid Image or validation errors.</returns>
    public static Result<Image> Create(string sourcePath, string? altText)
    {
        var errors = new List<IError>();

        // Create and validate UrlPath
        var urlPathResult = UrlPath.Create(sourcePath);
        if (urlPathResult.IsFailure)
        {
            errors.Add(urlPathResult.Error);
        }

        // Validate file extension
        var hasValidExtension = AllowedExtensions.Any(extension =>
            sourcePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        if (!hasValidExtension)
        {
            errors.Add(new ImageErrors.InvalidExtension(sourcePath));
        }

        // Validate alt text length
        if (altText is not null && altText.Length > AltTextMaxLength)
        {
            errors.Add(new ImageErrors.AltTextTooLong(altText));
        }

        // Value is guaranteed non-null here: FromValidation only calls factory when errors list is empty
        return Result<Image>.FromValidation(errors, () => new Image(urlPathResult.Value!, altText));
    }
}

public static class ImageErrors
{
    public sealed record InvalidExtension(string AttemptedPath) : IError
    {
        public string Code => "Image.InvalidExtension";
        public string Message => $"Image path must have a valid image extension ({string.Join(", ", AllowedExtensions)})";
        public string[] AllowedExtensions => Image.AllowedExtensions;
    }

    public sealed record AltTextTooLong(string AttemptedText) : IError
    {
        public string Code => "Image.AltTextTooLong";
        public string Message => $"Alt text cannot exceed {AltTextMaxLength} characters";
        public int AltTextMaxLength => Image.AltTextMaxLength;
    }
}
