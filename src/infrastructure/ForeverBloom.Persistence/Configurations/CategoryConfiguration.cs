using ForeverBloom.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.Property(c => c.Name)
            .IsRequired();

        builder.Property(c => c.CurrentSlug)
            .IsRequired();

        builder.OwnsOne(c => c.Image, imageBuilder =>
        {
            imageBuilder.Property(i => i.Source)
                .HasColumnName("image_path");

            imageBuilder.Property(i => i.AltText)
                .HasColumnName("image_alt_text")
                .HasMaxLength(Image.AltTextMaxLength);
        });

        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.PublishStatus)
            .HasDefaultValue(PublishStatus.Draft);

        builder.Property(c => c.Path)
            .IsRequired();

        // Self-referencing relationship for nested categories
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Index for parent-child relationships
        builder.HasIndex(c => c.ParentCategoryId);

        // Index for display order and publish status
        builder.HasIndex(c => new { c.PublishStatus, c.DisplayOrder });

        // Composite index for filtered hierarchical queries (ParentCategoryId filter + PublishStatus filter + DisplayOrder sort)
        builder.HasIndex(c => new { c.ParentCategoryId, c.PublishStatus, c.DisplayOrder });

        // Unique constraint on (Name, ParentCategoryId) - enforces name uniqueness at same level
        builder.HasIndex(c => new { c.Name, c.ParentCategoryId })
            .IsUnique();
    }
}
