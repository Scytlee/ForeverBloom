using ForeverBloom.Persistence.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Entities;

public sealed class Category : IAuditable, ISoftDeleteable
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string CurrentSlug { get; set; } = null!;
    public string? ImagePath { get; set; }

    // Hierarchical path using ltree
    public LTree Path { get; set; } = null!;

    // Self-referencing for nested categories
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    // Navigation property for Products
    public ICollection<Product> Products { get; set; } = new List<Product>();

    // Display order for sorting
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // IAuditable
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    // ISoftDeleteable
    public DateTimeOffset? DeletedAt { get; set; }
}
