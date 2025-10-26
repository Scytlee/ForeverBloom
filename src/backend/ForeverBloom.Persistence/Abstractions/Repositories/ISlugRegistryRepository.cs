using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Persistence.Abstractions.Repositories;

public interface ISlugRegistryRepository
{
    void InsertSlugRegistryEntry(SlugRegistryEntry entry);
    Task UpdateEntitySlugAsync(EntityType entityType, int entityId, string newSlug, CancellationToken cancellationToken = default);
}
