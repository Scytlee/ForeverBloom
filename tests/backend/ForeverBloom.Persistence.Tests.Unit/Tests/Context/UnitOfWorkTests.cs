using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.TestCorrelator;

namespace ForeverBloom.Persistence.Tests.Context;

[SuppressMessage("ReSharper", "AsyncVoidLambda")]
public sealed class UnitOfWorkTests
{
    private readonly ApplicationDbContext _mockDbContext;
    private readonly IRelationalDbContextTransaction _mockDbContextTransaction;
    private readonly DbConnection _mockDbConnection;

    public UnitOfWorkTests()
    {
        _mockDbContext = Substitute.For<ApplicationDbContext>();
        _mockDbContextTransaction = Substitute.For<IRelationalDbContextTransaction>();

        var mockDatabaseFacade = Substitute.For<DatabaseFacade>(_mockDbContext);
        mockDatabaseFacade.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDbContextTransaction>(_mockDbContextTransaction));
        _mockDbContext.Database.Returns(mockDatabaseFacade);

        var mockDbTransaction = Substitute.For<DbTransaction>();
        _mockDbContextTransaction.Instance.Returns(mockDbTransaction);
        _mockDbConnection = Substitute.For<DbConnection>();
        mockDbTransaction.Connection.Returns(_mockDbConnection);
        _mockDbConnection.State.Returns(ConnectionState.Open);
    }

    private UnitOfWork CreateUnitOfWork(ILogger<UnitOfWork>? logger = null)
    {
        return new UnitOfWork(_mockDbContext, logger ?? new NullLogger<UnitOfWork>());
    }

    private static Logger CreateSerilogLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestCorrelator()
            .CreateLogger();
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldStartNewTransaction_WhenNoTransactionStarted()
    {
        var sut = CreateUnitOfWork();

        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await _mockDbContext.Database.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldThrowInvalidOperationException_WhenTransactionAlreadyStarted()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbContext.Database.ClearReceivedCalls();

        var act = async () => await sut.BeginTransactionAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mockDbContext.Database.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowInvalidOperationException_WhenNoTransactionStarted()
    {
        var sut = CreateUnitOfWork();

        var act = async () => await sut.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowInvalidOperationException_WhenTransactionNotInValidState()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbConnection.State.Returns(ConnectionState.Closed);

        var act = async () => await sut.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().DisposeAsync();
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveChangesWithoutCommitting_WhenTransactionStartedAndNoErrorsOccur()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await sut.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().DisposeAsync();
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRethrowException_WhenSavingChangesFails()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbContext.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync<DbUpdateException>();

        var act = async () => await sut.SaveChangesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
        await _mockDbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().DisposeAsync();
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrowInvalidOperationException_WhenNoTransactionStarted()
    {
        var sut = CreateUnitOfWork();

        var act = async () => await sut.CompleteAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrowInvalidOperationException_WhenTransactionNotInValidState()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbConnection.State.Returns(ConnectionState.Closed);

        var act = async () => await sut.CompleteAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().DisposeAsync();
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task CompleteAsync_ShouldCommitAndDisposeTransaction_WhenTransactionStartedAndNoErrorsOccur()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await sut.CompleteAsync(TestContext.Current.CancellationToken);

        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task CompleteAsync_ShouldRethrowExceptionAndDisposeTransaction_WhenCommittingFails()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbContextTransaction.CommitAsync(Arg.Any<CancellationToken>()).ThrowsAsync<DbUpdateException>();

        var act = async () => await sut.CompleteAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_ShouldReturnGracefully_WhenNoTransactionStarted()
    {
        var sut = CreateUnitOfWork();

        var act = async () => await sut.RollbackAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_ShouldDisposeTransactionAndReturnGracefully_WhenTransactionNotInValidState()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbConnection.State.Returns(ConnectionState.Closed);

        var act = async () => await sut.RollbackAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.Received(1).DisposeAsync();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_ShouldRollbackAndDisposeTransaction_WhenTransactionStartedAndNoErrorsOccur()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await sut.RollbackAsync(TestContext.Current.CancellationToken);

        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_ShouldLogExceptionAndDisposeTransaction_WhenRollbackFails()
    {
        using var _ = TestCorrelator.CreateContext();
        await using var serilogLogger = CreateSerilogLogger();
        using var serilogLoggerFactory = new SerilogLoggerFactory(serilogLogger);

        var logger = serilogLoggerFactory.CreateLogger<UnitOfWork>();
        var sut = CreateUnitOfWork(logger);
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbContextTransaction.RollbackAsync(Arg.Any<CancellationToken>()).ThrowsAsync<DbUpdateException>();

        var act = async () => await sut.RollbackAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
        var logEvent = TestCorrelator.GetLogEventsFromCurrentContext()
            .FirstOrDefault(x => x.Level == LogEventLevel.Warning && x.Exception is DbUpdateException);
        logEvent.Should().NotBeNull();
    }

    [Fact]
    public async Task DisposeAsync_ShouldReturnGracefully_WhenNoTransactionStarted()
    {
        var sut = CreateUnitOfWork();

        var act = async () => await sut.DisposeAsync();

        await act.Should().NotThrowAsync();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeTransactionAndReturnGracefully_WhenTransactionNotInValidState()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbConnection.State.Returns(ConnectionState.Closed);

        var act = async () => await sut.DisposeAsync();

        await act.Should().NotThrowAsync();
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _mockDbContextTransaction.Received(1).DisposeAsync();
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldRollbackAndDisposeTransaction_WhenTransactionStartedAndNoErrorsOccur()
    {
        var sut = CreateUnitOfWork();
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await sut.DisposeAsync();

        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContextTransaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldLogExceptionAndDisposeTransaction_WhenRollbackFails()
    {
        using var _ = TestCorrelator.CreateContext();
        await using var serilogLogger = CreateSerilogLogger();
        using var serilogLoggerFactory = new SerilogLoggerFactory(serilogLogger);

        var logger = serilogLoggerFactory.CreateLogger<UnitOfWork>();
        var sut = CreateUnitOfWork(logger);
        await sut.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _mockDbContextTransaction.RollbackAsync(Arg.Any<CancellationToken>()).ThrowsAsync<DbUpdateException>();

        var act = async () => await sut.DisposeAsync();

        await act.Should().NotThrowAsync();
        Received.InOrder(async () =>
        {
            await _mockDbContextTransaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _mockDbContextTransaction.Received(1).DisposeAsync();
        });
        await _mockDbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mockDbContext.DidNotReceive().DisposeAsync();
        var logEvent = TestCorrelator.GetLogEventsFromCurrentContext()
            .FirstOrDefault(x => x.Level == LogEventLevel.Warning && x.Exception is DbUpdateException);
        logEvent.Should().NotBeNull();
    }

    [Fact]
    public void SuppressFeatures_ShouldForwardCallToApplicationDbContext()
    {
        var sut = CreateUnitOfWork();
        const PersistenceFeatures features = PersistenceFeatures.StampAuditTimestamps;

        sut.SuppressFeatures(features);

        _mockDbContext.Received(1).SuppressFeatures(features);
    }

    [Fact]
    public void OverrideFeatures_ShouldForwardCallToApplicationDbContext()
    {
        var sut = CreateUnitOfWork();
        const PersistenceFeatures features = PersistenceFeatures.None;

        sut.OverrideFeatures(features);

        _mockDbContext.Received(1).OverrideFeatures(features);
    }
}

public interface IRelationalDbContextTransaction : IDbContextTransaction, IInfrastructure<DbTransaction>
{

}
