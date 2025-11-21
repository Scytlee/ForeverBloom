using FluentAssertions;
using ForeverBloom.WebApi.Authentication.Settings;
using Microsoft.Extensions.Options;

namespace ForeverBloom.WebApi.UnitTests.Authentication.Settings;

public sealed class ApiKeyAuthenticationSettingsValidatorTests
{
    private readonly ApiKeyAuthenticationSettingsValidator _sut;

    public ApiKeyAuthenticationSettingsValidatorTests()
    {
        _sut = new ApiKeyAuthenticationSettingsValidator();
    }

    private static ApiKeyAuthenticationSettings CreateSettingsWith(
        string headerName = "X-Api-Key",
        string adminKey = "AdminKey",
        string[]? frontendKeys = null)
    {
        return new ApiKeyAuthenticationSettings
        {
            HeaderName = headerName,
            AdminKey = adminKey,
            FrontendKeys = frontendKeys ?? ["FrontendKey"]
        };
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenAllSettingsAreValid()
    {
        var settings = CreateSettingsWith();

        var result = _sut.Validate(null, settings);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenFrontendKeysContainsMultipleKeys()
    {
        var settings = CreateSettingsWith(frontendKeys: ["Key1", "Key2", "Key3"]);

        var result = _sut.Validate(null, settings);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnFailure_WhenHeaderNameIsInvalid(string? headerName)
    {
        var settings = CreateSettingsWith(headerName: headerName!);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HeaderName is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnFailure_WhenAdminKeyIsInvalid(string? adminKey)
    {
        var settings = CreateSettingsWith(adminKey: adminKey!);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AdminKey is required");
    }

    [Fact]
    public void Validate_ShouldReturnFailure_WhenFrontendKeysIsEmpty()
    {
        var settings = CreateSettingsWith(frontendKeys: []);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("FrontendKeys must contain at least one key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_ShouldReturnFailure_WhenFrontendKeysContainsInvalidKey(string? invalidKey)
    {
        var settings = CreateSettingsWith(frontendKeys: ["ValidKey", invalidKey!]);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("FrontendKeys contains empty or whitespace-only keys");
    }

    [Fact]
    public void Validate_ShouldReturnAllErrors_WhenMultipleSettingsAreInvalid()
    {
        var settings = CreateSettingsWith(
            headerName: "",
            adminKey: "   ",
            frontendKeys: []);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HeaderName is required");
        result.FailureMessage.Should().Contain("AdminKey is required");
        result.FailureMessage.Should().Contain("FrontendKeys must contain at least one key");
    }
}
