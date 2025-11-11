using System.Text;
using System.Text.Json;
using FluentAssertions;
using ForeverBloom.SharedKernel.Optional;

namespace ForeverBloom.SharedKernel.UnitTests.Optional;

public sealed class OptionalConverterTests
{
    [Fact]
    public void Read_ShouldCreateSetValue_WhenJsonTokenIsNull_ForReferenceType()
    {
        var json = "null"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();

        var converter = new OptionalConverter<string>();
        var result = converter.Read(ref reader, typeof(Optional<string>), new JsonSerializerOptions());

        result.IsSet.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Read_ShouldCreateSetValue_WhenJsonTokenIsNull_ForValueType()
    {
        var json = "null"u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();

        var converter = new OptionalConverter<int?>();
        var result = converter.Read(ref reader, typeof(Optional<int?>), new JsonSerializerOptions());

        result.IsSet.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Read_ShouldCreateSetValue_WhenJsonTokenHasValue()
    {
        var json = "\"hello\""u8.ToArray();
        var reader = new Utf8JsonReader(json);
        reader.Read();

        var converter = new OptionalConverter<string>();
        var result = converter.Read(ref reader, typeof(Optional<string>), new JsonSerializerOptions());

        result.IsSet.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Write_ShouldSerializeValue_WhenOptionalIsSet()
    {
        var opt = Optional<int>.FromValue(10);
        var converter = new OptionalConverter<int>();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        converter.Write(writer, opt, new JsonSerializerOptions());
        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("10");
    }

    [Theory]
    [InlineData(typeof(Optional<int>), true)]
    [InlineData(typeof(Optional<string>), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(List<int>), false)]
    public void Factory_CanConvert_ShouldReturnCorrectValue_ForType(Type type, bool expected)
    {
        var factory = new OptionalConverterFactory();

        var result = factory.CanConvert(type);

        result.Should().Be(expected);
    }

    [Fact]
    public void Factory_CreateConverter_ShouldReturnTypedConverter_ForOptionalType()
    {
        var factory = new OptionalConverterFactory();
        var typeToConvert = typeof(Optional<string>);

        var converter = factory.CreateConverter(typeToConvert, new JsonSerializerOptions());

        converter.Should().NotBeNull();
        converter.Should().BeOfType<OptionalConverter<string>>();
    }
}
