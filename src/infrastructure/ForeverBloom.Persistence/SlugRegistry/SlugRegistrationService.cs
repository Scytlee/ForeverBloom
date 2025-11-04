using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.SlugRegistry;

internal sealed class SlugRegistrationService : ISlugRegistrationService
{
    private readonly ApplicationDbContext _dbContext;

    public SlugRegistrationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterSlugAsync(EntityType entityType, long entityId, Slug slug, CancellationToken cancellationToken = default)
    {
        // Step 1: Deactivate all active slugs for this entity
        await _dbContext.Set<SlugRegistration>()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false), cancellationToken);

        // Step 2: Check if the new slug has been used by this entity before
        var historicalSlugEntry = await _dbContext.Set<SlugRegistration>()
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId && s.Slug == slug, cancellationToken);

        if (historicalSlugEntry is not null)
        {
            // Reactivate existing historical slug entry
            historicalSlugEntry.IsActive = true;
        }
        else
        {
            // Create new slug registration
            var newSlugEntry = new SlugRegistration
            {
                Slug = slug,
                EntityType = entityType,
                EntityId = entityId,
                IsActive = true
            };
            _dbContext.Set<SlugRegistration>().Add(newSlugEntry);
        }
    }

    public async Task UnregisterAllSlugsOfEntityAsync(EntityType entityType, long entityId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<SlugRegistration>()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> IsSlugAvailableAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Set<SlugRegistration>()
            .AnyAsync(s => s.Slug == slug, cancellationToken);

        return !exists; // Available if it doesn't exist
    }

    public async Task<bool> IsSlugAvailableForEntityAsync(Slug slug, EntityType entityType, long entityId, CancellationToken cancellationToken = default)
    {
        // Check if the slug is used by any other entity
        var usedByOtherEntity = await _dbContext.Set<SlugRegistration>()
            .AnyAsync(s => s.Slug == slug && !(s.EntityType == entityType && s.EntityId == entityId), cancellationToken);

        return !usedByOtherEntity; // Available if not used by another entity
    }
}
