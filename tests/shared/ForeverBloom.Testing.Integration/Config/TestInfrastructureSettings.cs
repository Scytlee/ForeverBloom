namespace ForeverBloom.Testing.Integration.Config;

public sealed record TestInfrastructureSettings
{
    public const string ConfigurationKeyName = "TestInfrastructure";

    public required string TemplateDatabase { get; init; }
    public required int MaxConcurrentDatabases { get; init; }
    public required int MaxPoolSize { get; init; }
}
