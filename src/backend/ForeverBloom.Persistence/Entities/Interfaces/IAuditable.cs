namespace ForeverBloom.Persistence.Entities.Interfaces;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
    uint RowVersion { get; }
}
