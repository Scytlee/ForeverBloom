using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.SharedKernel.UnitTests.Optional;

public sealed class OptionalJsonTypeInfoResolverTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        TypeInfoResolver = new OptionalJsonTypeInfoResolver()
    };

    [Fact]
    public void Serialize_ShouldOmitProperty_WhenOptionalIsUnset()
    {
        var dto = new PatchDto { Name = Optional<string?>.Unset };

        var json = JsonSerializer.Serialize(dto, _options);

        json.Should().Be("{}");
    }

    [Fact]
    public void Serialize_ShouldIncludeProperty_WhenOptionalIsSetToNonNull()
    {
        var dto = new PatchDto { Name = "Bob" };

        var json = JsonSerializer.Serialize(dto, _options);

        json.Should().Be("{\"Name\":\"Bob\"}");
    }

    [Fact]
    public void Serialize_ShouldIncludeNullProperty_WhenOptionalIsSetToNull()
    {
        var dto = new PatchDto { Name = Optional<string?>.FromValue(null) };

        var json = JsonSerializer.Serialize(dto, _options);

        json.Should().Be("{\"Name\":null}");
    }

    [Fact]
    public void Serialize_ShouldCombineShouldSerializeConditions_WhenPropertyHasExistingCondition()
    {
        // Arrange: set up resolver with TWO modifiers:
        // 1. Custom "IncludeValue" condition
        // 2. Optional resolver's IsSet condition
        var resolver = new OptionalJsonTypeInfoResolver();

        resolver.Modifiers.Insert(0, typeInfo =>
        {
            if (typeInfo.Type != typeof(ConditionalDto))
            {
                return;
            }

            var valueProperty = typeInfo.Properties
                .First(p => p.Name == nameof(ConditionalDto.Value));

            // Original condition: only serialize when IncludeValue is true
            valueProperty.ShouldSerialize = (obj, value) =>
            {
                var dto = (ConditionalDto)obj;
                return dto.IncludeValue;
            };
        });

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver
        };

        var dto = new ConditionalDto
        {
            Value = Optional<string>.FromValue("test"),
            IncludeValue = false
        };

        var json = JsonSerializer.Serialize(dto, options);

        json.Should().Be("{\"IncludeValue\":false}");
    }

    [Theory]
    [InlineData("{}", false, null)]
    [InlineData("{\"Name\":null}", true, null)]
    [InlineData("{\"Name\":\"Alice\"}", true, "Alice")]
    public void Deserialize_ShouldHandleAllCases_WhenPropertyIsMissingNullOrPresent(
        string json,
        bool expectedIsSet,
        string? expectedValue)
    {
        var dto = JsonSerializer.Deserialize<PatchDto>(json, _options);

        dto.Should().NotBeNull();
        dto.Name.IsSet.Should().Be(expectedIsSet);

        if (expectedIsSet)
        {
            dto.Name.Value.Should().Be(expectedValue);
        }
    }

    private sealed class PatchDto
    {
        public Optional<string?> Name { get; set; }
    }

    private sealed class ConditionalDto
    {
        public Optional<string> Value { get; set; }
        public bool IncludeValue { get; set; }
    }

    private sealed class ChainedResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);

            if (type == typeof(ConditionalDto))
            {
                var valueProperty = typeInfo.Properties.FirstOrDefault(p => p.Name == "Value");
                if (valueProperty is not null)
                {
                    var originalShouldSerialize = valueProperty.ShouldSerialize;
                    valueProperty.ShouldSerialize = (obj, value) =>
                    {
                        var dto = (ConditionalDto)obj;
                        return dto.IncludeValue && (originalShouldSerialize?.Invoke(obj, value) ?? true);
                    };
                }
            }

            var resolver = new OptionalJsonTypeInfoResolver();
            return resolver.GetTypeInfo(type, options);
        }
    }
}
