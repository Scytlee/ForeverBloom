using Microsoft.Extensions.Configuration;

namespace ForeverBloom.Testing.Common.Config;

public static class TestInfrastructureSettingsLoader
{
    private const string AppSettingsFileName = "appsettings.TestInfrastructure.json";

    private const string TemplateDatabaseOverrideVariable = "TESTINFRA_TEMPLATE_DATABASE";
    private const string MaxConcurrentDatabasesOverrideVariable = "TESTINFRA_MAX_CONCURRENT_DATABASES";
    private const string MaxPoolSizeOverrideVariable = "TESTINFRA_MAX_POOL_SIZE";

    public static TestInfrastructureSettings Load()
    {
        var configuration = BuildConfiguration();
        var settings = configuration
          .GetRequiredSection(TestInfrastructureSettings.ConfigurationKeyName)
          .Get<TestInfrastructureSettings>()
                       ?? throw new InvalidOperationException(
                         $"Failed to bind configuration section '{TestInfrastructureSettings.ConfigurationKeyName}' from '{AppSettingsFileName}'.");

        return ApplyEnvironmentOverrides(settings);
    }

    private static IConfigurationRoot BuildConfiguration() => new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile(AppSettingsFileName, optional: false, reloadOnChange: false)
      .Build();

    private static TestInfrastructureSettings ApplyEnvironmentOverrides(TestInfrastructureSettings settings)
    {
        var templateDatabase = ReadStringEnvironmentVariable(TemplateDatabaseOverrideVariable);
        var maxConcurrentDatabases = ReadIntEnvironmentVariable(MaxConcurrentDatabasesOverrideVariable);
        var maxPoolSize = ReadIntEnvironmentVariable(MaxPoolSizeOverrideVariable);

        if (templateDatabase is null && maxConcurrentDatabases is null && maxPoolSize is null)
        {
            return settings;
        }

        return new TestInfrastructureSettings
        {
            TemplateDatabase = templateDatabase ?? settings.TemplateDatabase,
            MaxConcurrentDatabases = maxConcurrentDatabases ?? settings.MaxConcurrentDatabases,
            MaxPoolSize = maxPoolSize ?? settings.MaxPoolSize
        };
    }

    private static string? ReadStringEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? ReadIntEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var parsedValue))
        {
            throw new InvalidOperationException($"Environment variable '{variableName}' must be an integer when set.");
        }

        return parsedValue;
    }
}
