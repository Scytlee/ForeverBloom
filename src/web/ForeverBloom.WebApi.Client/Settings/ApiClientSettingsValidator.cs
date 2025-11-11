using System.Text;
using Microsoft.Extensions.Options;

namespace ForeverBloom.WebApi.Client.Settings;

public sealed class ApiClientSettingsValidator : IValidateOptions<ApiClientSettings>
{
    public ValidateOptionsResult Validate(string? name, ApiClientSettings options)
    {
        var result = new StringBuilder();

        if (string.IsNullOrWhiteSpace(options.BasePath))
        {
            result.AppendLine($"{nameof(options.BasePath)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
        {
            result.AppendLine($"{nameof(options.ApiKeyHeaderName)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            result.AppendLine($"{nameof(options.ApiKey)} is required.");
        }

        return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
    }
}
