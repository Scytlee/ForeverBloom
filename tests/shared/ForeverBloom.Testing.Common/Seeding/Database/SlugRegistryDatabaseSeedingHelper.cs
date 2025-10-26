using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Testing.Common.Seeding.Database;

public static class SlugRegistryDatabaseSeedingHelper
{
    public static SlugRegistryEntry CreateSlugRegistryEntryWithoutSaving(
        string slug,
        int entityId,
        EntityType entityType = EntityType.Product,
        bool isActive = true)
    {
        var token = Guid.NewGuid().ToString("N");

        return new SlugRegistryEntry
        {
            Slug = slug ?? $"slug-{token[..20]}",
            EntityType = entityType,
            EntityId = entityId,
            IsActive = isActive
        };
    }

    public static async Task<SlugRegistryEntry> CreateSlugRegistryEntryAsync(
        this ApplicationDbContext context,
        string slug,
        int entityId,
        EntityType entityType = EntityType.Product,
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        var slugRegistryEntry = CreateSlugRegistryEntryWithoutSaving(slug, entityId, entityType, isActive);
        context.SlugRegistry.Add(slugRegistryEntry);
        await context.SaveChangesAsync(cancellationToken);
        return slugRegistryEntry;
    }
}
