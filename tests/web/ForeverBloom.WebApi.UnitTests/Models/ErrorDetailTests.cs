using FluentAssertions;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.WebApi.Models;

namespace ForeverBloom.WebApi.UnitTests.Models;

public sealed class ErrorDetailTests
{
    [Fact]
    public void FromError_ShouldCreateErrorDetailWithCodeAndMessage_WhenErrorHasNoAdditionalProperties()
    {
        var error = new SimpleError("Test.Error", "Test error message");

        var result = ErrorDetail.FromError(error);

        result.Code.Should().Be("Test.Error");
        result.Message.Should().Be("Test error message");
        result.Extensions.Should().BeNull();
    }

    [Fact]
    public void FromError_ShouldIncludeAdditionalPropertiesInExtensions_WhenErrorHasAdditionalProperties()
    {
        var error = new ErrorWithExtensions(
            "Validation.Failed",
            "Validation failed",
            "ValueOne",
            42);

        var result = ErrorDetail.FromError(error);

        result.Code.Should().Be("Validation.Failed");
        result.Message.Should().Be("Validation failed");
        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().ContainKey("propertyOne").WhoseValue.Should().Be("ValueOne");
        result.Extensions.Should().ContainKey("propertyTwo").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void FromError_ShouldSerializeAsAttemptedValue_WhenPropertyHasAttemptedValueAttribute()
    {
        var error = new ErrorWithAttemptedValue(
            "Slug.Invalid",
            "Invalid slug format",
            "invalid slug!");

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().ContainKey("attemptedValue").WhoseValue.Should().Be("invalid slug!");
        result.Extensions.Should().NotContainKey("invalidSlug");
    }

    [Fact]
    public void FromError_ShouldSerializeAsCurrentValue_WhenPropertyHasCurrentValueAttribute()
    {
        var error = new ErrorWithCurrentValue(
            "Limit.Exceeded",
            "Limit exceeded",
            100);

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().ContainKey("currentValue").WhoseValue.Should().Be(100L);
        result.Extensions.Should().NotContainKey("currentCount");
    }

    [Fact]
    public void FromError_ShouldSerializeBothValues_WhenErrorHasBothAttemptedAndCurrentValueAttributes()
    {
        var error = new ErrorWithBothValues(
            "Name.Conflict",
            "Name already exists",
            "NewName",
            "ExistingName");

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().ContainKey("attemptedValue").WhoseValue.Should().Be("NewName");
        result.Extensions.Should().ContainKey("currentValue").WhoseValue.Should().Be("ExistingName");
    }

    [Fact]
    public void FromError_ShouldExcludeNullProperties_WhenPropertiesAreNull()
    {
        var error = new ErrorWithNullableProperties(
            "Test.Error",
            "Test message",
            null,
            null);

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().BeNull();
    }

    [Fact]
    public void FromError_ShouldApplyCamelCaseToPropertyNames_WhenCreatingExtensions()
    {
        var error = new ErrorWithExtensions(
            "Test.Error",
            "Test message",
            "value",
            123);

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().ContainKey("propertyOne").And.NotContainKey("PropertyOne");
        result.Extensions.Should().ContainKey("propertyTwo").And.NotContainKey("PropertyTwo");
    }

    [Fact]
    public void FromError_ShouldCombineAttributesAndCamelCase_WhenErrorHasMixedProperties()
    {
        var error = new ErrorWithMixedProperties(
            "Mixed.Error",
            "Mixed error",
            "bad-value",
            "Extra context",
            5);

        var result = ErrorDetail.FromError(error);

        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().ContainKey("attemptedValue").WhoseValue.Should().Be("bad-value");
        result.Extensions.Should().ContainKey("additionalInfo").WhoseValue.Should().Be("Extra context");
        result.Extensions.Should().ContainKey("count").WhoseValue.Should().Be(5);
    }

    [Fact]
    public void FromError_ShouldThrowArgumentException_WhenErrorIsCompositeError()
    {
        var error1 = new SimpleError("Error.One", "First error");
        var error2 = new SimpleError("Error.Two", "Second error");
        var compositeError = new CompositeError([error1, error2]);

        var act = () => ErrorDetail.FromError(compositeError);

        act.Should().Throw<ArgumentException>()
            .WithMessage("ErrorDetail cannot be created from a CompositeError*")
            .And.ParamName.Should().Be("error");
    }

    private sealed record SimpleError(string Code, string Message) : IError;

    private sealed record ErrorWithExtensions(
        string Code,
        string Message,
        string PropertyOne,
        int PropertyTwo) : IError;

    private sealed record ErrorWithAttemptedValue(
        string Code,
        string Message,
        [property: AttemptedValue] string InvalidSlug) : IError;

    private sealed record ErrorWithCurrentValue(
        string Code,
        string Message,
        [property: CurrentValue] long CurrentCount) : IError;

    private sealed record ErrorWithBothValues(
        string Code,
        string Message,
        [property: AttemptedValue] string AttemptedName,
        [property: CurrentValue] string CurrentName) : IError;

    private sealed record ErrorWithNullableProperties(
        string Code,
        string Message,
        string? NullableProperty,
        int? NullableNumber) : IError;

    private sealed record ErrorWithMixedProperties(
        string Code,
        string Message,
        [property: AttemptedValue] string InvalidValue,
        string AdditionalInfo,
        int Count) : IError;
}
