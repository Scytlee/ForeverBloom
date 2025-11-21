using System.Reflection;
using System.Text.Json.Serialization;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Extensions;

namespace ForeverBloom.WebApi.Models;

/// <summary>
/// Represents a single error detail with code, message, and additional properties as extensions.
/// Used for errors in BadRequestProblemDetails.
/// </summary>
public sealed class ErrorDetail
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Additional error properties are flattened here.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; init; }

    /// <summary>
    /// Creates an ErrorDetail from an error by serializing all properties (except Code/Message) to extensions.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>An ErrorDetail with all error properties serialized.</returns>
    /// <exception cref="ArgumentException">Thrown if error is a CompositeError.</exception>
    public static ErrorDetail FromError(IError error)
    {
        if (error is CompositeError)
        {
            throw new ArgumentException(
                "ErrorDetail cannot be created from a CompositeError",
                nameof(error));
        }

        var properties = error.GetType().GetProperties()
            .Where(p => p.Name != nameof(Code)
                     && p.Name != nameof(Message)
                     && p.Name != "EqualityContract"  // Skip record infrastructure
                     && p.CanRead);

        var extensions = BuildExtensionsDictionary(properties, error);

        return new ErrorDetail
        {
            Code = error.Code,
            Message = error.Message,
            Extensions = extensions.Count > 0 ? extensions : null
        };
    }

    private static Dictionary<string, object?> BuildExtensionsDictionary(
        IEnumerable<PropertyInfo> properties,
        IError error)
    {
        var extensions = new Dictionary<string, object?>();

        var propertyDetails = GetPropertyDetails(properties, error);

        foreach (var propertyDetail in propertyDetails)
        {
            var value = propertyDetail.Property.GetValue(error);
            if (value is null)
            {
                continue;
            }

            var jsonPropertyName = propertyDetail switch
            {
                { IsAttemptedValue: true } => "attemptedValue",
                { IsCurrentValue: true } => "currentValue",
                _ => propertyDetail.Property.Name.ToCamelCase()
            };

            extensions[jsonPropertyName] = value;
        }

        return extensions;
    }

    private static IEnumerable<PropertyDetails> GetPropertyDetails(
        IEnumerable<PropertyInfo> properties,
        IError error)
    {
        // For convenience, attributes might be defined on constructor parameters instead of properties.
        // To avoid having to include the "property:" prefix on attributes or declaring explicit properties,
        // we're checking both constructors and properties for attributes, and zipping the results together.
        return error.GetType().GetConstructors()
            // We assume that error types will always have a single constructor
            // Worst case scenario is that only code and message are serialized
            .FirstOrDefault()?
            .GetParameters()
            // Merge constructor parameter attributes with property attributes
            .RightJoin(properties,
                parameter => parameter.Name!,
                property => property.Name,
                (parameter, property) => (
                    Property: property,
                    Attributes: parameter is null
                        ? property.CustomAttributes.ToArray()
                        : property.CustomAttributes.Concat(parameter.CustomAttributes).ToArray()))
            .Select(p => new PropertyDetails
            {
                Property = p.Property,
                IsAttemptedValue = p.Attributes.Any(a => a.AttributeType == typeof(AttemptedValueAttribute)),
                IsCurrentValue = p.Attributes.Any(a => a.AttributeType == typeof(CurrentValueAttribute)),
                // IsMetaData = p.Attributes.Any(a => a.AttributeType == typeof(MetadataAttribute))
            }) ?? [];
    }

    private sealed class PropertyDetails
    {
        public required PropertyInfo Property { get; init; } = null!;
        public required bool IsAttemptedValue { get; init; }
        public required bool IsCurrentValue { get; init; }
        // public required bool IsMetaData { get; init; }
    }
}
