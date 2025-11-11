namespace ForeverBloom.Tools.DatabaseMigrator;

internal sealed record DatabaseMigratorSettings
{
    public const string ConfigurationSectionName = "DatabaseMigrator";

    /// <summary>
    /// When true, this service applies pending EF Core migrations automatically.
    /// When false, it only checks and fails fast if migrations are outstanding.
    /// </summary>
    public bool ApplyPendingMigrations { get; init; }
}
