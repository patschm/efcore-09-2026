using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class SpecificationDefinitionConfiguration : IEntityTypeConfiguration<SpecificationDefinition>
{
    public void Configure(EntityTypeBuilder<SpecificationDefinition> entity)
    {
        entity.ToTable("SpecificationDefinition");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SpecificationDefinitionId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.ProductGroupId)
            .HasConversion(id => id.Value, value => new ProductGroupId(value))
            .IsRequired();

        entity.Property(e => e.Key).HasMaxLength(300).IsRequired();
        entity.Property(e => e.Name).HasMaxLength(512).IsRequired();
        entity.Property(e => e.Unit).HasMaxLength(100);
        entity.Property(e => e.Type).HasMaxLength(100);

        entity.HasIndex(e => e.ProductGroupId);
        entity.HasIndex(e => new { e.ProductGroupId, e.Key }).IsUnique();
    }
}
