namespace ForeverBloom.Domain.Abstractions;

public interface ISoftDeleteable
{
    DateTimeOffset? DeletedAt { get; }
    bool IsDeleted => DeletedAt.HasValue;
}
