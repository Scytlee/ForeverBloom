using FluentAssertions;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Repositories;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Repositories;

public sealed class SlugRegistryRepositoryTests : DatabaseTestClassBase
{
    public SlugRegistryRepositoryTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task UpdateEntitySlugAsync_ShouldCreateNewActiveSlugEntry_WhenSlugIsNew()
    {
        var sut = new SlugRegistryRepository(DbContext);
        const int entityId = 1;
        const EntityType entityType = EntityType.Category;
        var initialSlug = $"initial-slug-{_testId:N}"[..20];
        var newSlug = $"new-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: initialSlug,
            entityId: entityId,
            entityType: entityType,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        await sut.UpdateEntitySlugAsync(entityType, entityId, newSlug, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var allSlugsForEntity = await DbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .ToListAsync(TestContext.Current.CancellationToken);
        allSlugsForEntity.Should().HaveCount(2);
        var originalSlugEntry = allSlugsForEntity.Single(s => s.Slug == initialSlug);
        originalSlugEntry.IsActive.Should().BeFalse();
        var newSlugEntry = allSlugsForEntity.Single(s => s.Slug == newSlug);
        newSlugEntry.IsActive.Should().BeTrue();
        newSlugEntry.EntityType.Should().Be(entityType);
        newSlugEntry.EntityId.Should().Be(entityId);
    }

    [Fact]
    public async Task UpdateEntitySlugAsync_ShouldReactivateHistoricalSlug_WhenSlugWasUsedBefore()
    {
        var sut = new SlugRegistryRepository(DbContext);
        const int entityId = 1;
        const EntityType entityType = EntityType.Category;
        var initialSlug = $"initial-slug-{_testId:N}"[..20];
        var historicalSlug = $"hist-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: initialSlug,
            entityId: entityId,
            entityType: entityType,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: historicalSlug,
            entityId: entityId,
            entityType: entityType,
            isActive: false,
            cancellationToken: TestContext.Current.CancellationToken);

        await sut.UpdateEntitySlugAsync(entityType, entityId, historicalSlug, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify database state
        DbContext.ChangeTracker.Clear();
        var allSlugsForEntity = await DbContext.SlugRegistry
            .AsNoTracking()
            .Where(s => s.EntityType == entityType && s.EntityId == entityId)
            .ToListAsync(TestContext.Current.CancellationToken);
        allSlugsForEntity.Should().HaveCount(2);
        var initialEntry = allSlugsForEntity.Single(s => s.Slug == initialSlug);
        initialEntry.IsActive.Should().BeFalse();
        var historicalEntry = allSlugsForEntity.Single(s => s.Slug == historicalSlug);
        historicalEntry.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SlugRegistry_ShouldEnforceGlobalSlugUniqueness_WhenDuplicateSlugCreatedAcrossEntityTypes()
    {
        const int productId = 11;
        const int categoryId = 12;
        var sharedSlug = $"shared-slug-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: sharedSlug,
            entityId: productId,
            entityType: EntityType.Product,
            cancellationToken: TestContext.Current.CancellationToken);
        var duplicateEntry = SlugRegistryDatabaseSeedingHelper.CreateSlugRegistryEntryWithoutSaving(
            slug: sharedSlug,
            entityId: categoryId,
            entityType: EntityType.Category);

        DbContext.SlugRegistry.Add(duplicateEntry);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
