using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Persistence.Abstractions.Repositories;

public interface ICategoryRepository
{
    void InsertCategory(Category category);
    void UpdateCategory(Category category);
    void ArchiveCategory(Category category);
    void RestoreCategory(Category category);
    void DeleteCategory(int categoryId, uint rowVersion);
}
