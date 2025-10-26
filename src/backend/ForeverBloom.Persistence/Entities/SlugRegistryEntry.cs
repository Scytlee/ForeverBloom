using ForeverBloom.Domain.Shared.Models;

namespace ForeverBloom.Persistence.Entities;

public sealed class SlugRegistryEntry
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public EntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public bool IsActive { get; set; }
}
