using System.Text;
using Microsoft.Extensions.Options;

namespace ForeverBloom.Api.Authentication.Settings;

public sealed class ApiKeyAuthenticationSettingsValidator : IValidateOptions<ApiKeyAuthenticationSettings>
{
    public ValidateOptionsResult Validate(string? name, ApiKeyAuthenticationSettings options)
    {
        var result = new StringBuilder();

        if (string.IsNullOrWhiteSpace(options.HeaderName))
        {
            result.AppendLine($"{nameof(options.HeaderName)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AdminKey))
        {
            result.AppendLine($"{nameof(options.AdminKey)} is required.");
        }

        if (options.FrontendKeys.Length == 0)
        {
            result.AppendLine($"{nameof(options.FrontendKeys)} must contain at least one key.");
        }
        else if (options.FrontendKeys.Any(string.IsNullOrWhiteSpace))
        {
            result.AppendLine($"{nameof(options.FrontendKeys)} contains empty or whitespace-only keys.");
        }

        return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
    }
}
