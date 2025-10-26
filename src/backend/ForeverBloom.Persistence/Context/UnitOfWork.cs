using System.Data;
using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Entities.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ForeverBloom.Persistence.Context;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext dbContext, ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        EnsureTransactionNotStarted();

        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureTransactionStarted();
        if (!TransactionIsActive())
        {
            throw new InvalidOperationException("Transaction has already been completed or is not active");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        EnsureTransactionStarted();
        if (!TransactionIsActive())
        {
            throw new InvalidOperationException("Transaction has already been completed or is not active");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeAndClearTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (TransactionIsActive())
        {
            await AttemptRollbackAsync(cancellationToken);
        }

        await DisposeAndClearTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (TransactionIsActive())
        {
            await AttemptRollbackAsync();
        }

        await DisposeAndClearTransactionAsync();
    }

    public void SuppressFeatures(PersistenceFeatures featuresToSuppress)
    {
        _dbContext.SuppressFeatures(featuresToSuppress);
    }

    public void OverrideFeatures(PersistenceFeatures newFeatureSet)
    {
        _dbContext.OverrideFeatures(newFeatureSet);
    }

    #region Private helpers
    private void EnsureTransactionNotStarted()
    {
        if (_transaction is not null)
        {
            // The class doesn't know what to do with the existing transaction,
            // so, instead of deciding for itself, we throw an exception
            throw new InvalidOperationException("Transaction has already been started");
        }
    }

    [MemberNotNull(nameof(_transaction))]
    private void EnsureTransactionStarted()
    {
        if (_transaction is null)
        {
            // An action has been invoked that requires a transaction, but one hasn't been started yet.
            // Therefore, it's an invalid call, and we throw an exception
            throw new InvalidOperationException("Transaction has not been started");
        }
    }

    [MemberNotNullWhen(true, nameof(_transaction))]
    private bool TransactionIsActive()
    {
        if (_transaction is null)
        {
            return false;
        }

        var connection = _transaction.GetDbTransaction().Connection;
        return connection is not null && connection.State == ConnectionState.Open;
    }
    private async ValueTask DisposeAndClearTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private async Task AttemptRollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // This is a best-effort operation, so we don't want to throw an exception if it fails.
                // However, the exception must be logged
                _logger.LogWarning(ex, "An error occurred while attempting to rollback the transaction");
            }
        }
    }
    #endregion
}
