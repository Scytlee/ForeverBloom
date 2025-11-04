using System.Reflection;
using System.Text.Json.Serialization;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Extensions;

namespace ForeverBloom.WebApi.Models;

/// <summary>
/// Represents a single error detail with code, message, and additional properties as extensions.
/// Used for errors in BadRequestProblemDetails.
/// </summary>
public class ErrorDetail
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
            .Where(p => p.Name != nameof(Code) && p.Name != nameof(Message) && p.CanRead);

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

        foreach (var property in properties)
        {
            var value = property.GetValue(error);
            if (value is null)
            {
                continue;
            }

            // Convert property name to camelCase
            var propertyName = property.Name.ToCamelCase();
            extensions[propertyName] = value;
        }

        return extensions;
    }
}
