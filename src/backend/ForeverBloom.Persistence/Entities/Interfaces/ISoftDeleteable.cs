namespace ForeverBloom.Persistence.Entities.Interfaces;

public interface ISoftDeleteable
{
    DateTimeOffset? DeletedAt { get; }
}
