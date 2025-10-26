using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", ApplicationDbContext.BusinessSchema);

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(CategoryValidation.Constants.NameMaxLength);

        builder.Property(c => c.Description)
            .HasMaxLength(CategoryValidation.Constants.DescriptionMaxLength);

        builder.Property(c => c.CurrentSlug)
            .IsRequired()
            .HasMaxLength(SlugValidation.Constants.MaxLength);

        builder.Property(c => c.ImagePath)
            .HasMaxLength(CategoryValidation.Constants.ImagePathMaxLength);

        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Self-referencing relationship for nested categories
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Index for parent-child relationships
        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("IX_Categories_ParentCategoryId");

        // Index for display order and active status
        builder.HasIndex(c => new { c.IsActive, c.DisplayOrder })
            .HasDatabaseName("IX_Categories_IsActive_DisplayOrder");

        // Composite index for filtered hierarchical queries (ParentCategoryId filter + Active filter + DisplayOrder sort)
        builder.HasIndex(c => new { c.ParentCategoryId, c.IsActive, c.DisplayOrder })
            .HasDatabaseName("IX_Categories_ParentCategoryId_IsActive_DisplayOrder");

        // Unique constraint on (Name, ParentCategoryId) - enforces name uniqueness at same level
        builder.HasIndex(c => new { c.Name, c.ParentCategoryId })
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name_ParentCategoryId_Unique");
    }
}
