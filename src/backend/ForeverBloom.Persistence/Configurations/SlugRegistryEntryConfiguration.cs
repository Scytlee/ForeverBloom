using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForeverBloom.Persistence.Configurations;

public class SlugRegistryEntryConfiguration : IEntityTypeConfiguration<SlugRegistryEntry>
{
    public void Configure(EntityTypeBuilder<SlugRegistryEntry> builder)
    {
        builder.ToTable("SlugRegistry", ApplicationDbContext.BusinessSchema);

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(SlugValidation.Constants.MaxLength);

        builder.Property(s => s.EntityType)
            .IsRequired();

        builder.Property(s => s.EntityId)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        // Global unique constraint on Slug - enforces global uniqueness across all entity types
        builder.HasIndex(s => s.Slug)
            .IsUnique()
            .HasDatabaseName("IX_SlugRegistry_Slug_Unique");

        // Composite unique constraint on (EntityType, EntityId, IsActive) where IsActive = true
        // Ensures only one active slug per entity at any time
        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.IsActive })
            .IsUnique()
            .HasDatabaseName("IX_SlugRegistry_EntityType_EntityId_IsActive_Unique")
            .HasFilter($"\"{nameof(SlugRegistryEntry.IsActive)}\" = true");

        // Performance indexes
        builder.HasIndex(s => new { s.EntityType, s.EntityId })
            .HasDatabaseName("IX_SlugRegistry_EntityType_EntityId");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_SlugRegistry_IsActive");
    }
}
