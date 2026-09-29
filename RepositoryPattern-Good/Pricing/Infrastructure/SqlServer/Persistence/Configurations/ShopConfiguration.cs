using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> entity)
    {
        entity.ToTable("Shop");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ShopId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(1024).IsRequired();

        // Url has exactly one underlying field, so it maps to a single column via
        // HasConversion - same pattern as the strongly-typed ids - rather than the
        // multi-column "complex type" API, which turned out not to correctly support
        // an optional (nullable) struct like Url? for Logo below.
        entity.Property(e => e.Url)
            .HasConversion(u => u.Value, v => new Url(v))
            .HasMaxLength(850)
            .IsRequired();

        entity.Property(e => e.Logo)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => v == null ? (Url?)null : new Url(v))
            .HasMaxLength(4000);

        entity.HasIndex(e => e.Url).IsUnique();
    }
}
