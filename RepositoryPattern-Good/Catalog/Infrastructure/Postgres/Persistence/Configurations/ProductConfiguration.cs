using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Product");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(1024).IsRequired();

        entity.Property(e => e.BrandId)
            .HasConversion(id => id.Value, value => new BrandId(value))
            .IsRequired();

        entity.Property(e => e.ProductGroupId).HasConversion(
            id => id.HasValue ? id.Value.Value : (int?)null,
            value => value.HasValue ? new ProductGroupId(value.Value) : (ProductGroupId?)null);

        entity.Property(e => e.ImageUrl)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => Url.TryCreate(v))
            .HasMaxLength(2048);

        entity.HasMany(e => e.SpecificationValues)
            .WithOne()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasIndex(e => e.BrandId);
        entity.HasIndex(e => e.ProductGroupId);

        entity.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(e => e.BrandId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne<ProductGroup>()
            .WithMany()
            .HasForeignKey(e => e.ProductGroupId);

        entity.Property<long>("RowVersion").IsConcurrencyToken().HasDefaultValue(0L);
    }
}
