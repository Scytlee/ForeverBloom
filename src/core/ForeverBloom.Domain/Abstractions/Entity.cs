namespace ForeverBloom.Domain.Abstractions;

/// <summary>
/// Base class for all domain entities providing identity and audit trail.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Timestamp when the entity was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the entity was last updated (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>
    /// Concurrency token managed by EF Core for optimistic concurrency control.
    /// </summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Initializes the entity with audit timestamps.
    /// </summary>
    /// <param name="timestamp">The timestamp to use for CreatedAt and UpdatedAt (must be UTC).</param>
    protected Entity(DateTimeOffset timestamp)
    {
        CreatedAt = timestamp;
        UpdatedAt = timestamp;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    protected Entity()
    {
    }
}
