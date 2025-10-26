using FluentAssertions;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Testing.Common.Seeding.Database;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ForeverBloom.Persistence.Tests.Context;

public sealed class UnitOfWorkTests : DatabaseTestClassBase
{
    public UnitOfWorkTests(DatabaseFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task CompleteAsync_ShouldPersistData_WhenTransactionCommits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var persisted = await verificationContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, cancellationToken);

        persisted.Should().NotBeNull();
        persisted.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowAndNotPersist_WhenTransactionNotStarted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var unitOfWork = CreateUnitOfWork();

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        var act = async () => await unitOfWork.SaveChangesAsync(cancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var exists = await verificationContext.Categories
          .AsNoTracking()
          .AnyAsync(c => c.Id == category.Id, cancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RollbackAsync_ShouldDiscardData_WhenTransactionRollsBack()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.RollbackAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var exists = await verificationContext.Categories
          .AsNoTracking()
          .AnyAsync(c => c.Id == category.Id, cancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ShouldRollbackAndDispose_WhenTransactionLeftOpen()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.DisposeAsync();

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var exists = await verificationContext.Categories
          .AsNoTracking()
          .AnyAsync(c => c.Id == category.Id, cancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldThrowButKeepOriginalActive_WhenCalledTwice()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var act = async () => await unitOfWork.BeginTransactionAsync(cancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var persisted = await verificationContext.Categories
          .AsNoTracking()
          .SingleAsync(c => c.Id == category.Id, cancellationToken);

        persisted.Should().NotBeNull();
        persisted.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task RollbackAsync_ShouldUndoPendingChanges_WhenExceptionOccurs()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var category = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        DbContext.Categories.Add(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        DbContext.ChangeTracker.Clear();

        var conflictingCategory = CategoryDatabaseSeedingHelper.CreateCategoryWithoutSaving();
        conflictingCategory.Id = category.Id;
        DbContext.Categories.Add(conflictingCategory);

        var act = async () => await unitOfWork.SaveChangesAsync(cancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();

        await unitOfWork.RollbackAsync(cancellationToken);

        DbContext.ChangeTracker.Clear();

        await using var verificationContext = CreateVerificationContext();
        var exists = await verificationContext.Categories
          .AsNoTracking()
          .AnyAsync(c => c.Id == category.Id, cancellationToken);

        exists.Should().BeFalse();
    }

    private UnitOfWork CreateUnitOfWork(ILogger<UnitOfWork>? logger = null)
    {
        return new UnitOfWork(DbContext, logger ?? new NullLogger<UnitOfWork>());
    }

    private ApplicationDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseNpgsql(DatabaseConnectionString)
          .Options;

        return new ApplicationDbContext(options);
    }
}
