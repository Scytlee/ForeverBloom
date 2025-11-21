using FluentAssertions;
using ForeverBloom.WebUI.RazorPages.Settings;
using Microsoft.Extensions.Options;

namespace ForeverBloom.WebUI.RazorPages.UnitTests.Settings;

public sealed class GoogleAnalyticsSettingsValidatorTests
{
    private readonly GoogleAnalyticsSettingsValidator _sut;

    public GoogleAnalyticsSettingsValidatorTests()
    {
        _sut = new GoogleAnalyticsSettingsValidator();
    }

    private static GoogleAnalyticsSettings CreateSettingsWith(
        bool enabled = false,
        string? trackingId = null)
    {
        return new GoogleAnalyticsSettings
        {
            Enabled = enabled,
            TrackingId = trackingId
        };
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenDisabled()
    {
        var settings = CreateSettingsWith();

        var result = _sut.Validate(null, settings);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenEnabledWithValidTrackingId()
    {
        var settings = CreateSettingsWith(enabled: true, trackingId: "G-XXXXXXXXXX");

        var result = _sut.Validate(null, settings);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnFailure_WhenEnabledWithInvalidTrackingId(string? trackingId)
    {
        var settings = CreateSettingsWith(enabled: true, trackingId: trackingId);

        var result = _sut.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("TrackingId is required when enabling Google Analytics");
    }
}
