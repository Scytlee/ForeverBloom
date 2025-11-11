using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Repositories;

internal sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }

    public async Task<Product?> GetByIdIncludingArchivedAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Product>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
