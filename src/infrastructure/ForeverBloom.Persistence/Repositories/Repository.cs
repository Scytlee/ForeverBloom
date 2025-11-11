using ForeverBloom.Domain.Abstractions;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Repositories;

internal abstract class Repository<T> where T : Entity
{
    protected readonly ApplicationDbContext DbContext;

    protected Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<T>()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public void Add(T entity)
    {
        DbContext.Add(entity);
    }

    public void Delete(T entity)
    {
        DbContext.Remove(entity);
    }
}
