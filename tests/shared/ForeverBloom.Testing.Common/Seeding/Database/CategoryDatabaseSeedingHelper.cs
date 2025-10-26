using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Testing.Common.Seeding.Database;

public static class CategoryDatabaseSeedingHelper
{
    public static Category CreateCategoryWithoutSaving(
      string? name = null,
      string? slug = null,
      string? description = null,
      string? imagePath = null,
      int? displayOrder = null,
      bool? isActive = null,
      int? parentCategoryId = null)
    {
        var token = Guid.NewGuid().ToString("N");
        slug ??= $"category-{token[..20]}";

        return new Category
        {
            Name = name ?? $"Category {token[..20]}",
            CurrentSlug = slug,
            Path = new LTree(slug),
            Description = description,
            ImagePath = imagePath,
            DisplayOrder = displayOrder ?? 0,
            IsActive = isActive ?? true,
            ParentCategoryId = parentCategoryId
        };
    }

    public static async Task<Category> CreateCategoryAsync(
      this ApplicationDbContext context,
      string? name = null,
      string? slug = null,
      string? description = null,
      string? imagePath = null,
      int? displayOrder = null,
      bool? isActive = null,
      int? parentCategoryId = null,
      CancellationToken cancellationToken = default)
    {
        var category = CreateCategoryWithoutSaving(name, slug, description, imagePath, displayOrder, isActive, parentCategoryId);
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }
}
