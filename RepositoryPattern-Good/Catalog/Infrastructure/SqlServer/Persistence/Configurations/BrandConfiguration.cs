using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Configurations;

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

        entity.Property(e => e.Website)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => v == null ? (Url?)null : new Url(v))
            .HasMaxLength(2048);

        entity.Property(e => e.Logo)
            .HasConversion(
                u => u.HasValue ? u.Value.Value : null,
                v => v == null ? (Url?)null : new Url(v))
            .HasMaxLength(4000);
    }
}
