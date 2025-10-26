namespace ForeverBloom.Persistence.Entities.Interfaces;

[Flags]
public enum PersistenceFeatures
{
    None = 0,
    StampAuditTimestamps = 1,
    All = StampAuditTimestamps,
}
