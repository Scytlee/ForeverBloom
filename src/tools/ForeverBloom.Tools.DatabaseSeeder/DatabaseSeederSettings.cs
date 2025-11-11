namespace ForeverBloom.Tools.DatabaseSeeder;

internal sealed record DatabaseSeederSettings
{
    public const string ConfigurationSectionName = "DatabaseSeeder";

    /// <summary>
    /// Controls whether the seeding workflow should actually run.
    /// </summary>
    public bool EnableSeeding { get; init; }
}
