using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _clock;

    public ProductRepository(ApplicationDbContext dbContext, TimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public void InsertProduct(Product product)
    {
        _dbContext.Products.Add(product);
    }

    public void UpdateProduct(Product product)
    {
        _dbContext.Products.Update(product);
    }

    public void ArchiveProduct(Product product)
    {
        product.DeletedAt = _clock.GetUtcNow();
    }

    public void RestoreProduct(Product product)
    {
        product.DeletedAt = null;
    }

    public void DeleteProduct(int productId, uint rowVersion)
    {
        var product = new Product { Id = productId, RowVersion = rowVersion };
        _dbContext.Products.Remove(product);
    }

    public void ReplaceProductImages(Product product, IEnumerable<ProductImage> newImages)
    {
        product.Images.Clear();
        foreach (var image in newImages)
        {
            product.Images.Add(image);
        }

        // Force RowVersion change
        _dbContext.Entry(product).Property(p => p.UpdatedAt).IsModified = true;
    }
}
