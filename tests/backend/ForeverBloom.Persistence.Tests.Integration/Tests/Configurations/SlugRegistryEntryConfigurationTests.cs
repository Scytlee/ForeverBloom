using FluentAssertions;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Configurations;

public sealed class SlugRegistryEntryConfigurationTests : DatabaseTestClassBase
{
    public SlugRegistryEntryConfigurationTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task SlugRegistryTable_ShouldAllowOnlyOneActiveSlugPerEntity()
    {
        const int entityId = 42;
        const EntityType entityType = EntityType.Product;
        var activeSlug = $"active-{_testId:N}"[..20];
        var nextSlug = $"next-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: activeSlug,
            entityId: entityId,
            entityType: entityType,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        var conflictingEntry = SlugRegistryDatabaseSeedingHelper.CreateSlugRegistryEntryWithoutSaving(
            slug: nextSlug,
            entityId: entityId,
            entityType: entityType,
            isActive: true);
        DbContext.SlugRegistry.Add(conflictingEntry);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SlugRegistryTable_ShouldEnforceGlobalSlugUniqueness()
    {
        var sharedSlug = $"shared-{_testId:N}"[..20];
        await DbContext.CreateSlugRegistryEntryAsync(
            slug: sharedSlug,
            entityId: 1,
            entityType: EntityType.Category,
            isActive: true,
            cancellationToken: TestContext.Current.CancellationToken);

        var duplicateSlug = SlugRegistryDatabaseSeedingHelper.CreateSlugRegistryEntryWithoutSaving(
            slug: sharedSlug,
            entityId: 2,
            entityType: EntityType.Product,
            isActive: false);
        DbContext.SlugRegistry.Add(duplicateSlug);
        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
