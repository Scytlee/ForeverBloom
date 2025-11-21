using System.Data;
using FluentAssertions;
using ForeverBloom.Application.Abstractions.Behaviors;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ForeverBloom.Application.UnitTests.Abstractions.Behaviors;

public sealed class TransactionalCommandBehaviorTests
{
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Handle_ShouldUseDefaultTransactionSettings_WhenCommandHasNoOverrides()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();

        _ = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        _unitOfWork.Isolation.Should().Be(TransactionSettings.Default.Isolation);
        _unitOfWork.LockTimeout.Should().Be(TransactionSettings.Default.LockTimeout);
        _unitOfWork.StatementTimeout.Should().Be(TransactionSettings.Default.StatementTimeout);
    }

    [Fact]
    public async Task Handle_ShouldCommitAndReturnSuccess_WhenHandlerSucceeds()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();
        var handlerCalls = 0;

        var response = await behavior.Handle(
            command,
            _ =>
            {
                handlerCalls++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        handlerCalls.Should().Be(1);
        response.Should().BeSuccess();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
        _unitOfWork.CommitDecision.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureWithoutCommit_WhenHandlerFails()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Failure(new ApplicationErrors.RequiredFieldMissing("Field"))),
            CancellationToken.None);

        response.Should().BeFailure();
        response.Should().HaveError<ApplicationErrors.RequiredFieldMissing>();
        _unitOfWork.CommitDecision.Should().BeFalse();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldApplyTransactionOverrides_WhenCommandProvidesSettings()
    {
        var behavior = CreateBehavior<OverriddenCommand>();
        var overrides = new TransactionSettings
        {
            Isolation = IsolationLevel.Serializable,
            LockTimeout = TimeSpan.FromSeconds(2),
            StatementTimeout = TimeSpan.FromSeconds(5)
        };
        var command = new OverriddenCommand(overrides);

        _ = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        _unitOfWork.Isolation.Should().Be(overrides.Isolation);
        _unitOfWork.LockTimeout.Should().Be(overrides.LockTimeout);
        _unitOfWork.StatementTimeout.Should().Be(overrides.StatementTimeout);
    }

    [Fact]
    public async Task Handle_ShouldTranslateEfConcurrencyException_WhenUnitOfWorkThrows()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();
        _unitOfWork.ExceptionToThrow = new DbUpdateConcurrencyException();

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        response.Should().BeFailure();
        response.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
        _unitOfWork.CommitDecision.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldTranslatePostgresSerializationFailure_WhenUnitOfWorkThrows()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();
        _unitOfWork.ExceptionToThrow = CreatePostgresSerializationFailure();

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        response.Should().BeFailure();
        response.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
        _unitOfWork.CommitDecision.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldTranslateEfConcurrencyException_WhenHandlerThrows()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();

        var response = await behavior.Handle(
            command,
            _ => throw new DbUpdateConcurrencyException(),
            CancellationToken.None);

        response.Should().BeFailure();
        response.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
        _unitOfWork.CommitDecision.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldTranslatePostgresSerializationFailure_WhenHandlerThrows()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();

        var response = await behavior.Handle(
            command,
            _ => throw CreatePostgresSerializationFailure(),
            CancellationToken.None);

        response.Should().BeFailure();
        response.Should().HaveError<ApplicationErrors.ConcurrencyConflict>();
        _unitOfWork.ExecuteCallCount.Should().Be(1);
        _unitOfWork.CommitDecision.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldRethrowPostgresException_WhenNotSerializationFailure()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();
        const string sqlState = "23505";
        var otherException = CreatePostgresException(sqlState);
        _unitOfWork.ExceptionToThrow = otherException;

        Func<Task> act = () => behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        await act.Should().ThrowExactlyAsync<PostgresException>()
            .Where(ex => ex.SqlState == sqlState);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToUnitOfWork()
    {
        var behavior = CreateBehavior<TestCommand>();
        var command = new TestCommand();
        using var cts = new CancellationTokenSource();

        _ = await behavior.Handle(
            command,
            _ => Task.FromResult(Result.Success()),
            cts.Token);

        _unitOfWork.LastCancellationToken.Should().Be(cts.Token);
    }

    private TransactionalCommandBehavior<TCommand, Result> CreateBehavior<TCommand>()
        where TCommand : IBaseCommand
    {
        return new TransactionalCommandBehavior<TCommand, Result>(_unitOfWork);
    }

    private static PostgresException CreatePostgresSerializationFailure() =>
        CreatePostgresException(PostgresErrorCodes.SerializationFailure);

    private static PostgresException CreatePostgresException(string sqlState) =>
        new("Postgres error", "ERROR", "ERROR", sqlState);

    private sealed record TestCommand : IBaseCommand;

    private sealed record OverriddenCommand(TransactionSettings TransactionSettings)
        : IBaseCommand, IWithTransactionOverrides;

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int ExecuteCallCount { get; private set; }
        public bool? CommitDecision { get; private set; }
        public IsolationLevel Isolation { get; private set; } = IsolationLevel.ReadCommitted;
        public TimeSpan? LockTimeout { get; private set; }
        public TimeSpan? StatementTimeout { get; private set; }
        public Exception? ExceptionToThrow { get; set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public DbSet<T> Set<T>() where T : class =>
            throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public async Task<TResult> ExecuteTransactionalAsync<TResult>(
            Func<CancellationToken, Task<TResult>> work,
            Func<TResult, bool>? commitCondition = null,
            IsolationLevel isolation = IsolationLevel.ReadCommitted,
            TimeSpan? lockTimeout = null,
            TimeSpan? statementTimeout = null,
            CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            Isolation = isolation;
            LockTimeout = lockTimeout;
            StatementTimeout = statementTimeout;
            LastCancellationToken = cancellationToken;

            if (ExceptionToThrow is not null)
            {
                var exception = ExceptionToThrow;
                ExceptionToThrow = null;
                throw exception;
            }

            var result = await work(cancellationToken);
            CommitDecision = commitCondition?.Invoke(result);
            return result;
        }
    }
}
