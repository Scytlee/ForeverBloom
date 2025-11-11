namespace ForeverBloom.WebUI.RazorPages.Settings;

internal sealed class GoogleAnalyticsSettings
{
    public const string ConfigurationKeyName = "GoogleAnalytics";

    public required bool Enabled { get; set; } = false;
    public string? TrackingId { get; set; }
}
