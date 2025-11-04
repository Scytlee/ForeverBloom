using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Persistence.SlugRegistry;

internal sealed class SlugRegistration
{
    public long Id { get; set; }
    public Slug Slug { get; set; } = null!;
    public EntityType EntityType { get; set; }
    public long EntityId { get; set; }
    public bool IsActive { get; set; }
}
