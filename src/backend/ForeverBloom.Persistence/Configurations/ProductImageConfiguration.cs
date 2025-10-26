using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", ApplicationDbContext.BusinessSchema);

        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.Id).ValueGeneratedOnAdd();

        builder.Property(pi => pi.ImagePath)
            .IsRequired()
            .HasMaxLength(ProductValidation.Constants.ImagePathMaxLength);

        builder.Property(pi => pi.IsPrimary)
            .IsRequired();

        builder.Property(pi => pi.DisplayOrder)
            .IsRequired();

        builder.Property(pi => pi.AltText)
            .HasMaxLength(ProductValidation.Constants.AltTextMaxLength);

        // Foreign key relationship to Product
        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Essential indexes for PostgreSQL
        builder.HasIndex(pi => pi.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        builder.HasIndex(pi => new { pi.ProductId, pi.DisplayOrder })
            .HasDatabaseName("IX_ProductImages_ProductId_DisplayOrder");
    }
}
