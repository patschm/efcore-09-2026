using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.ValueObjects;

namespace WebShop.Pricing.Infrastructure.Postgres.Persistence.Configurations;

public sealed class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> entity)
    {
        entity.ToTable("Price");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PriceId(value))
            .ValueGeneratedNever();

        // ProductId is a reference into the Catalog bounded context - this project has
        // no dependency on WebShop.Catalog, so it's mapped as a plain converted column,
        // not a navigable relationship.
        entity.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .IsRequired();

        entity.Property(e => e.ShopId)
            .HasConversion(id => id.Value, value => new ShopId(value))
            .IsRequired();

        // The real columns are a single double each - no currency was ever recorded, so unlike
        // Money's general shape (Amount + Currency), Currency here is assumed fixed at "EUR"
        // in code rather than round-tripped through the database. If this data ever becomes
        // genuinely multi-currency, that assumption breaks silently - there's no column to
        // catch it.
        entity.Property(e => e.ShopPrice)
            .HasConversion(m => m.Amount, amount => new Money(amount, "EUR"))
            .HasColumnName("ShopPrice")
            .IsRequired();

        entity.Property(e => e.ShippingPrice)
            .HasConversion(m => m.Amount, amount => new Money(amount, "EUR"))
            .HasColumnName("ShippingPrice")
            .IsRequired();

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.ShopId);
        entity.HasIndex(e => new { e.ShopId, e.ProductId }).IsUnique();

        entity.HasOne<Shop>()
            .WithMany()
            .HasForeignKey(e => e.ShopId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
