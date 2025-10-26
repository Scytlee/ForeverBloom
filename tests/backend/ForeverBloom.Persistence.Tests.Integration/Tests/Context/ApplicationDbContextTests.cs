using FluentAssertions;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using ForeverBloom.Persistence.Entities.Interfaces;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using ForeverBloom.Testing.Common.Seeding.Database;
using ForeverBloom.Testing.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Tests.Context;

public sealed class ApplicationDbContextTests : DatabaseTestClassBase
{
    public ApplicationDbContextTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public void Model_ShouldMapEntitiesToBusinessSchema()
    {
        var entityTypes = DbContext.Model.GetEntityTypes()
          .Where(et => et.ClrType.Assembly == typeof(Category).Assembly)
          .ToList();

        entityTypes.Should().NotBeEmpty();
        entityTypes.Should().AllSatisfy(et => et.GetSchema().Should().Be(ApplicationDbContext.BusinessSchema));
    }

    [Fact]
    public void Model_ShouldConfigureTimestampColumnsWithPrecision()
    {
        var timestampProperties = DbContext.Model.GetEntityTypes()
          .SelectMany(et => et.GetProperties())
          .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?))
          .ToList();

        timestampProperties.Should().NotBeEmpty();
        timestampProperties.Should().AllSatisfy(property =>
        {
            property.GetColumnType().Should().Be("timestamp(6) with time zone");
            property.GetPrecision().Should().Be(6);
        });
    }

    [Fact]
    public async Task SoftDeletedEntities_ShouldBeFilteredByDefaultQueries()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        category.DeletedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var result = await DbContext.Categories
          .AsNoTracking()
          .SingleOrDefaultAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeletedEntities_ShouldSurfaceWhenIgnoringQueryFilters()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        category.DeletedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var result = await DbContext.Categories
          .IgnoreQueryFilters()
          .AsNoTracking()
          .SingleOrDefaultAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RowVersion_ShouldIncrement_WhenEntityIsUpdated()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var originalRowVersion = category.RowVersion;

        category.Name = $"{category.Name}-updated";
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DbContext.ChangeTracker.Clear();

        var refreshed = await DbContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        originalRowVersion.Should().BeGreaterThan(0);
        refreshed.RowVersion.Should().BeGreaterThan(originalRowVersion);
    }

    [Fact]
    public async Task Database_ShouldNotHavePendingMigrations()
    {
        var pendingMigrations = await DbContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task SavingNewAuditableEntity_ShouldStampUtcTimestamps()
    {
        var lowerBound = TimeProvider.System.GetUtcNow();

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var upperBound = TimeProvider.System.GetUtcNow();

        category.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        category.UpdatedAt.Offset.Should().Be(TimeSpan.Zero);
        category.CreatedAt.Should().BeOnOrAfter(lowerBound);
        category.CreatedAt.Should().BeOnOrBefore(upperBound);
        category.UpdatedAt.Should().Be(category.CreatedAt);
        category.RowVersion.Should().BeGreaterThan(0u);

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        persisted.CreatedAt.Should().BeCloseTo(category.CreatedAt, TemporalTolerances.DatabaseTimestamp);
        persisted.UpdatedAt.Should().BeCloseTo(category.UpdatedAt, TemporalTolerances.DatabaseTimestamp);
        persisted.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task UpdatingAuditableEntity_ShouldPreserveCreatedAndRefreshUpdated()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        var createdAt = category.CreatedAt;
        var previousUpdatedAt = category.UpdatedAt;
        var previousRowVersion = category.RowVersion;

        category.DisplayOrder = 5;

        var lowerBound = TimeProvider.System.GetUtcNow();
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var upperBound = TimeProvider.System.GetUtcNow();

        category.CreatedAt.Should().Be(createdAt);
        category.UpdatedAt.Should().BeOnOrAfter(lowerBound);
        category.UpdatedAt.Should().BeOnOrBefore(upperBound);
        category.UpdatedAt.Should().BeAfter(previousUpdatedAt);
        category.RowVersion.Should().BeGreaterThan(previousRowVersion);

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        persisted.CreatedAt.Should().BeCloseTo(createdAt, TemporalTolerances.DatabaseTimestamp);
        persisted.UpdatedAt.Should().BeCloseTo(category.UpdatedAt, TemporalTolerances.DatabaseTimestamp);
        persisted.RowVersion.Should().Be(category.RowVersion);
    }

    [Fact]
    public async Task SoftDeletingAuditableEntity_ShouldMirrorDeletedAtInUpdatedAt()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        var deletionTimestamp = DateTimeOffset.UtcNow.AddMinutes(1);
        category.DeletedAt = deletionTimestamp;

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        category.DeletedAt.Should().Be(deletionTimestamp);
        category.UpdatedAt.Should().Be(deletionTimestamp);

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Categories
          .IgnoreQueryFilters()
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        persisted.DeletedAt.Should().BeCloseTo(deletionTimestamp, TemporalTolerances.DatabaseTimestamp);
        persisted.UpdatedAt.Should().BeCloseTo(deletionTimestamp, TemporalTolerances.DatabaseTimestamp);
    }

    [Fact]
    public async Task RestoringSoftDeletedEntity_ShouldRefreshUpdatedAt()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        var deletedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        category.DeletedAt = deletedAt;
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var lowerBound = TimeProvider.System.GetUtcNow();

        category.DeletedAt = null;
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var upperBound = TimeProvider.System.GetUtcNow();

        category.DeletedAt.Should().BeNull();
        category.UpdatedAt.Should().BeOnOrAfter(lowerBound);
        category.UpdatedAt.Should().BeOnOrBefore(upperBound);

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, TestContext.Current.CancellationToken);

        persisted.DeletedAt.Should().BeNull();
        persisted.UpdatedAt.Should().BeCloseTo(category.UpdatedAt, TemporalTolerances.DatabaseTimestamp);
    }

    [Fact]
    public async Task SavingNonUtcDate_ShouldThrowInvalidOperationException()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        category.DeletedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));

        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("*UTC DateTimeOffset*");
    }

    [Fact]
    public async Task SuppressFeatures_ShouldNotStampAuditTimestamps_WhenStampAuditTimestampsFeatureIsSuppressed()
    {
        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-5);
        var originalUpdatedAt = DateTimeOffset.UtcNow.AddDays(-3);
        category.CreatedAt = originalCreatedAt;
        category.UpdatedAt = originalUpdatedAt;

        DbContext.SuppressFeatures(PersistenceFeatures.StampAuditTimestamps);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        category.CreatedAt.Should().Be(originalCreatedAt);
        category.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public async Task ApplicationDbContext_ShouldValidateUtcTimestamps()
    {
        var category = await DbContext.CreateCategoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        category.DeletedAt = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));

        var act = async () => await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
          .WithMessage("*UTC DateTimeOffset*");
    }
}
