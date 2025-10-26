using System.Text;
using Microsoft.Extensions.Options;

namespace ForeverBloom.DatabaseManager;

internal sealed class DatabaseManagerSettingsValidator : IValidateOptions<DatabaseManagerSettings>
{
    public ValidateOptionsResult Validate(string? name, DatabaseManagerSettings options)
    {
        var result = new StringBuilder();

        if (!options.SkipInitialization)
        {
            if (string.IsNullOrWhiteSpace(options.AppRole))
            {
                result.AppendLine($"{nameof(options.AppRole)} is required for database initialization.");
            }

            if (string.IsNullOrWhiteSpace(options.AppUserName))
            {
                result.AppendLine($"{nameof(options.AppUserName)} is required for database initialization.");
            }

            if (string.IsNullOrWhiteSpace(options.AppUserPassword))
            {
                result.AppendLine($"{nameof(options.AppUserPassword)} is required for database initialization.");
            }
        }

        return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
    }
}
