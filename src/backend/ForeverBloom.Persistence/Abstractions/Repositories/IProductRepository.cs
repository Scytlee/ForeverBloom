using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Persistence.Abstractions.Repositories;

public interface IProductRepository
{
    void InsertProduct(Product product);
    void UpdateProduct(Product product);
    void ArchiveProduct(Product product);
    void RestoreProduct(Product product);
    void DeleteProduct(int productId, uint rowVersion);
    void ReplaceProductImages(Product product, IEnumerable<ProductImage> newImages);
}
