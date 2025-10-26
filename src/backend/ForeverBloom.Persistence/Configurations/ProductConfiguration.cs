using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", ApplicationDbContext.BusinessSchema);

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(ProductValidation.Constants.NameMaxLength);

        builder.Property(p => p.SeoTitle)
            .HasMaxLength(ProductValidation.Constants.SeoTitleMaxLength);

        builder.Property(p => p.FullDescription)
            .HasMaxLength(ProductValidation.Constants.FullDescriptionMaxLength);

        builder.Property(p => p.MetaDescription)
            .HasMaxLength(ProductValidation.Constants.MetaDescriptionMaxLength);

        builder.Property(p => p.CurrentSlug)
            .IsRequired()
            .HasMaxLength(SlugValidation.Constants.MaxLength);

        builder.Property(p => p.Price)
            .HasPrecision(12, 2); // PostgreSQL NUMERIC(12,2) - supports up to 9,999,999,999.99

        builder.Property(p => p.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.PublishStatus)
            .HasDefaultValue(PublishStatus.Draft);

        builder.Property(p => p.Availability)
            .HasDefaultValue(ProductAvailabilityStatus.Available);

        // Foreign key relationship to Category
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with ProductImages
        builder.HasMany(p => p.Images)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Essential indexes for PostgreSQL
        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        builder.HasIndex(p => new { p.CategoryId, p.PublishStatus, p.DisplayOrder })
            .HasDatabaseName("IX_Products_CategoryId_PublishStatus_DisplayOrder");
    }
}
