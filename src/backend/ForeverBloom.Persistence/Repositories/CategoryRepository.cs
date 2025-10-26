using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;

namespace ForeverBloom.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _clock;

    public CategoryRepository(ApplicationDbContext dbContext, TimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public void InsertCategory(Category category)
    {
        _dbContext.Categories.Add(category);
    }

    public void UpdateCategory(Category category)
    {
        _dbContext.Categories.Update(category);
    }

    public void ArchiveCategory(Category category)
    {
        category.DeletedAt = _clock.GetUtcNow();
    }

    public void RestoreCategory(Category category)
    {
        category.DeletedAt = null;
    }

    public void DeleteCategory(int categoryId, uint rowVersion)
    {
        var category = new Category { Id = categoryId, RowVersion = rowVersion };
        _dbContext.Categories.Remove(category);
    }
}
