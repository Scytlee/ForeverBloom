using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Repositories;

public sealed class SlugRegistryRepository : ISlugRegistryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SlugRegistryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void InsertSlugRegistryEntry(SlugRegistryEntry entry)
    {
        _dbContext.SlugRegistry.Add(entry);
    }

    public async Task UpdateEntitySlugAsync(EntityType entityType, int entityId, string newSlug, CancellationToken cancellationToken = default)
    {
        // Step 1: Deactivate all active slugs for this entity using ExecuteUpdate
        await _dbContext.SlugRegistry
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false), cancellationToken);

        // Step 2: Check if the new slug has been used by this entity before
        var historicalSlugEntry = await _dbContext.SlugRegistry
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId && s.Slug == newSlug, cancellationToken);

        if (historicalSlugEntry is not null)
        {
            // Scenario 2: Reactivate existing historical slug entry
            historicalSlugEntry.IsActive = true;
        }
        else
        {
            // Scenario 1: Create new slug entry
            var newSlugEntry = new SlugRegistryEntry
            {
                Slug = newSlug,
                EntityType = entityType,
                EntityId = entityId,
                IsActive = true
            };
            _dbContext.SlugRegistry.Add(newSlugEntry);
        }
    }
}
