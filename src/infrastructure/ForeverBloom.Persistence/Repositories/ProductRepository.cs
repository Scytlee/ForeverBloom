using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Persistence.Context;

namespace ForeverBloom.Persistence.Repositories;

internal sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }
}
