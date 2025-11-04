using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ForeverBloom.WebApi.Extensions;

namespace ForeverBloom.WebApi.Models;

/// <summary>
/// Represents a single validation error detail with code, message, attempted value, and custom state.
/// Used for request validation errors in ValidationProblemDetails.
/// </summary>
public sealed class ValidationErrorDetail : ErrorDetail
{
    private const int MaxAttemptedValueLength = 200;

    [JsonPropertyName("attemptedValue")]
    public object? AttemptedValue { get; init; }

    /// <summary>
    /// Creates a ValidationErrorDetail with custom state converted to extensions.
    /// </summary>
    [SetsRequiredMembers]
    public ValidationErrorDetail(string code, string message, object? attemptedValue = null, object? customState = null)
    {
        Code = code;
        Message = message;
        AttemptedValue = attemptedValue;

        if (customState is null || !customState.GetType().IsAnonymousType())
        {
            Extensions = null;
            return;
        }

        var properties = customState.GetType().GetProperties();
        var extensions = new Dictionary<string, object?>(properties.Length);

        foreach (var property in properties)
        {
            var propertyName = property.Name.ToCamelCase();
            extensions[propertyName] = property.GetValue(customState);
        }

        Extensions = extensions.Count > 0 ? extensions : null;
    }

    /// <summary>
    /// Creates a ValidationErrorDetail from a FluentValidation ValidationFailure.
    /// Truncates attemptedValue if it's a string longer than 200 characters.
    /// </summary>
    public static ValidationErrorDetail FromValidationFailure(FluentValidation.Results.ValidationFailure failure)
    {
        var attemptedValue = failure.AttemptedValue;

        // Truncate string attempted values if they exceed the max length
        if (attemptedValue is string { Length: > MaxAttemptedValueLength } str)
        {
            attemptedValue = string.Concat(str.AsSpan(0, MaxAttemptedValueLength), "...");
        }

        return new ValidationErrorDetail(failure.ErrorCode, failure.ErrorMessage, attemptedValue, failure.CustomState);
    }
}
