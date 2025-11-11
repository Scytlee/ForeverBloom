using ForeverBloom.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.Property(p => p.Name)
            .IsRequired();

        builder.Property(p => p.CurrentSlug)
            .IsRequired();

        builder.Property(p => p.PublishStatus)
            .HasDefaultValue(PublishStatus.Draft);

        builder.Property(p => p.Availability)
            .HasDefaultValue(ProductAvailabilityStatus.ComingSoon);

        // Foreign key relationship to Category
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned collection for ProductImages
        builder.OwnsMany(p => p.Images, imagesBuilder =>
        {
            imagesBuilder.ToTable("product_images");

            imagesBuilder.HasKey(pi => pi.Id);

            imagesBuilder.Property(pi => pi.Id)
                .ValueGeneratedOnAdd();

            imagesBuilder.Property(pi => pi.IsPrimary)
                .IsRequired();

            imagesBuilder.Property(pi => pi.DisplayOrder)
                .IsRequired();

            imagesBuilder.OwnsOne(pi => pi.Image, imageBuilder =>
            {
                imageBuilder.Property(i => i.Source)
                    .HasColumnName("image_path");

                imageBuilder.Property(i => i.AltText)
                    .HasColumnName("image_alt_text")
                    .HasMaxLength(Image.AltTextMaxLength);
            });

            imagesBuilder.HasIndex("ProductId", nameof(ProductImage.DisplayOrder));

            // Enforce at most one primary image per product
            imagesBuilder.HasIndex("ProductId")
                .IsUnique()
                .HasFilter("is_primary = true");
        });

        builder.HasIndex(p => p.CategoryId);

        builder.HasIndex(p => new { p.CategoryId, p.PublishStatus });
    }
}
