namespace ForeverBloom.DatabaseManager;

internal sealed class DatabaseManagerSettings
{
    public const string ConfigurationKeyName = "DatabaseManager";
    public required string AppRole { get; set; }
    public required string AppUserName { get; set; }
    public required string AppUserPassword { get; set; }

    public bool SkipInitialization { get; set; } = false;
    public bool SkipMigration { get; set; } = false;
    public bool SkipSeeding { get; set; } = false;
}
