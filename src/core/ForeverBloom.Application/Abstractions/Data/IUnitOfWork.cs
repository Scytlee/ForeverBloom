using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Application.Abstractions.Data;

public interface IUnitOfWork
{
    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteTransactionalAsync<T>(
        Func<CancellationToken, Task<T>> work,
        Func<T, bool>? commitCondition = null,
        IsolationLevel isolation = IsolationLevel.ReadCommitted,
        TimeSpan? lockTimeout = null,
        TimeSpan? statementTimeout = null,
        CancellationToken cancellationToken = default);
}
