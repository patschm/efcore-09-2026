using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> entity)
    {
        entity.ToTable("Brand");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new BrandId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(512).IsRequired();

        // The real column is unbounded text, not varchar - no HasMaxLength here.
        entity.Property(e => e.Website)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => Url.TryCreate(v));

        entity.Property(e => e.Logo)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => Url.TryCreate(v))
            .HasMaxLength(4000);

        // ParentId (brand hierarchy) exists in the real table but is deliberately left unmapped -
        // Brand doesn't model hierarchy yet. RowVersion is mapped without a domain property at
        // all: it's a technical concurrency token, not something Brand's own behavior needs to
        // know about - see CatalogPgContext.BumpRowVersions for how it actually gets used.
        entity.Property<long>("RowVersion").IsConcurrencyToken().HasDefaultValue(0L);
    }
}
