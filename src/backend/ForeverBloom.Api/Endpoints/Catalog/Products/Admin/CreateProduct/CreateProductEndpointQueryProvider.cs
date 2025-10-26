using ForeverBloom.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;

public sealed class CreateProductEndpointQueryProvider : ICreateProductEndpointQueryProvider
{
    private readonly ApplicationDbContext _dbContext;

    public CreateProductEndpointQueryProvider(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default)
    {
        var slugExists = await _dbContext.SlugRegistry
            .AsNoTracking()
            .AnyAsync(s => s.Slug == slug, cancellationToken);

        return !slugExists;
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}
