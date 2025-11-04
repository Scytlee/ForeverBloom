using ForeverBloom.Domain.Shared;

namespace ForeverBloom.Application.Abstractions.SlugRegistry;

/// <summary>
/// Service for managing slug registrations across all entity types.
/// Ensures global slug uniqueness and handles slug lifecycle (creation, updates, deletion).
/// </summary>
public interface ISlugRegistrationService
{
    /// <summary>
    /// Registers a slug for an entity, making it the active slug.
    /// If the entity already has an active slug, it will be deactivated first.
    /// If the slug was previously used by this entity, it will be reactivated.
    /// Otherwise, a new slug registration will be created.
    /// </summary>
    /// <param name="entityType">The type of entity (Product, Category, etc.)</param>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="slug">The slug to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterSlugAsync(EntityType entityType, long entityId, Slug slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all slug registrations for an entity.
    /// This permanently frees up all slugs that were used by this entity.
    /// Should only be called when an entity is permanently deleted.
    /// </summary>
    /// <param name="entityType">The type of entity (Product, Category, etc.)</param>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnregisterAllSlugsOfEntityAsync(EntityType entityType, long entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a slug is available for use (not already registered).
    /// </summary>
    /// <param name="slug">The slug to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slug is available, false if it's already in use</returns>
    Task<bool> IsSlugAvailableAsync(Slug slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a slug is available for a specific entity to use.
    /// Returns true if the slug is not in use, or if it was only used by this specific entity (allowing reuse of own historical slugs).
    /// </summary>
    /// <param name="slug">The slug to check</param>
    /// <param name="entityType">The type of entity (Product, Category, etc.)</param>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slug is available for this entity to use, false if it's used by another entity</returns>
    Task<bool> IsSlugAvailableForEntityAsync(Slug slug, EntityType entityType, long entityId, CancellationToken cancellationToken = default);
}
