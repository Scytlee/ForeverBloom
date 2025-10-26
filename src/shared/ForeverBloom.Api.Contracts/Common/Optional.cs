using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ForeverBloom.Api.Contracts.Common;

[JsonConverter(typeof(OptionalConverterFactory))]
public readonly struct Optional<T>
{
    public bool IsSet { get; }
    private readonly T _value;
    public T Value => IsSet ? _value : throw new InvalidOperationException("Optional value is not set.");

    private Optional(T value)
    {
        _value = value;
        IsSet = true;
    }

    public static Optional<T> Unset { get; } = new();
    public static Optional<T> FromValue(T value) => new(value);

    public override string ToString() => IsSet ? Value?.ToString() ?? "null" : "[Unset]";

    public static implicit operator Optional<T>(T value) => FromValue(value);
}

public class OptionalConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Optional<T>.FromValue(default!);
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.FromValue(value!);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.IsSet)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }

    public override bool HandleNull => true;
}

public class OptionalConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(OptionalConverter<>).MakeGenericType(valueType),
            BindingFlags.Instance | BindingFlags.Public, null, null, null)!;
    }
}

public sealed class OptionalJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public OptionalJsonTypeInfoResolver()
    {
        Modifiers.Add(ConfigureOptionalProperties);
    }

    private static void ConfigureOptionalProperties(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in jsonTypeInfo.Properties)
        {
            if (!IsOptional(property.PropertyType) || property.Get is null)
            {
                continue;
            }

            var getter = property.Get;
            var isSetProperty = property.PropertyType.GetProperty(nameof(Optional<int>.IsSet));
            if (isSetProperty is null)
            {
                continue;
            }

            var shouldSerialize = CreateShouldSerializeDelegate(getter, isSetProperty);

            if (property.ShouldSerialize is null)
            {
                property.ShouldSerialize = Wrap(shouldSerialize);
            }
            else
            {
                var existing = property.ShouldSerialize;
                property.ShouldSerialize = (obj, state) => existing(obj, state) && shouldSerialize(obj);
            }
        }
    }

    private static bool IsOptional(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);

    private static Func<object, bool> CreateShouldSerializeDelegate(Func<object, object?> getter, PropertyInfo isSetProperty)
    {
        return obj =>
        {
            var optional = getter(obj);
            if (optional is null)
            {
                return false;
            }

            return (bool)isSetProperty.GetValue(optional)!;
        };
    }

    private static Func<object, object?, bool> Wrap(Func<object, bool> predicate) => (obj, _) => predicate(obj);
}
