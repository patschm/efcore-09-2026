using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> entity)
    {
        entity.ToTable("ProductGroup");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ProductGroupId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(512).IsRequired();

        entity.Property(e => e.ParentId).HasConversion(
            id => id.HasValue ? id.Value.Value : (int?)null,
            value => value.HasValue ? new ProductGroupId(value.Value) : (ProductGroupId?)null);

        entity.Property(e => e.ImageUrl)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => v == null ? (Url?)null : new Url(v))
            .HasMaxLength(2048);

        entity.HasMany(e => e.SpecificationDefinitions)
            .WithOne()
            .HasForeignKey(e => e.ProductGroupId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasIndex(e => e.ParentId);

        entity.HasOne<ProductGroup>()
            .WithMany()
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
