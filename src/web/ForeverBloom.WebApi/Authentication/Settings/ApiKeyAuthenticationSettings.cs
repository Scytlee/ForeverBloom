namespace ForeverBloom.WebApi.Authentication.Settings;

public sealed record ApiKeyAuthenticationSettings
{
    public const string ConfigurationKeyName = "ApiKeys";

    public required string HeaderName { get; init; }
    public required string AdminKey { get; init; }
    public required string[] FrontendKeys { get; init; }
}
