using System.Text;
using Microsoft.Extensions.Options;

namespace ForeverBloom.WebUI.RazorPages.Settings;

internal sealed class GoogleAnalyticsSettingsValidator : IValidateOptions<GoogleAnalyticsSettings>
{
    public ValidateOptionsResult Validate(string? name, GoogleAnalyticsSettings options)
    {
        var result = new StringBuilder();

        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.TrackingId))
            {
                result.AppendLine($"{nameof(options.TrackingId)} is required when enabling Google Analytics.");
            }
        }

        return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
    }
}
