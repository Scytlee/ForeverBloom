using FluentValidation;
using FluentValidation.Results;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Abstractions.Validation;

/// <summary>
/// Base validation extension methods and helpers.
/// </summary>
internal static class ValidationExtensions
{
    /// <summary>
    /// Creates a ValidationFailure from a domain error with standardised Code, Message, and AttemptedValue.
    /// </summary>
    /// <param name="context">The validation context containing property information</param>
    /// <param name="error">The domain error containing Code and Message</param>
    /// <param name="attemptedValue">The value that was being validated</param>
    /// <param name="propertyPathOverride">The value that will override the property path from the context</param>
    /// <returns>A ValidationFailure with ErrorCode, Message, and AttemptedValue populated</returns>
    internal static ValidationFailure CreateFailureFromError<T>(ValidationContext<T> context, IError error, object? attemptedValue, string? propertyPathOverride = null)
    {
        return new ValidationFailure(propertyPathOverride ?? context.PropertyPath, error.Message, attemptedValue)
        {
            ErrorCode = error.Code
        };
    }
}
