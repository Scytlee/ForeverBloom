using ForeverBloom.Persistence.SlugRegistry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

internal class SlugRegistrationConfiguration : IEntityTypeConfiguration<SlugRegistration>
{
    public void Configure(EntityTypeBuilder<SlugRegistration> builder)
    {
        builder.ToTable("slug_registry");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Slug)
            .IsRequired();

        builder.Property(s => s.EntityType)
            .IsRequired();

        builder.Property(s => s.EntityId)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        // Global unique constraint on Slug - enforces global uniqueness across all entity types
        builder.HasIndex(s => s.Slug)
            .IsUnique();

        // Composite unique constraint on (EntityType, EntityId, IsActive) where IsActive = true
        // Ensures only one active slug per entity at any time
        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.IsActive })
            .IsUnique()
            .HasFilter("\"is_active\" = true");

        // Performance indexes
        builder.HasIndex(s => new { s.EntityType, s.EntityId });

        builder.HasIndex(s => s.IsActive);
    }
}
