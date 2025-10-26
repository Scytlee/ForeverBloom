namespace ForeverBloom.ApiClient.Settings;

public sealed record ApiClientSettings
{
    public const string ConfigurationKeyName = "ApiClient";

    public required string BasePath { get; init; }
    public required string ApiKeyHeaderName { get; init; }
    public required string ApiKey { get; init; }
}
