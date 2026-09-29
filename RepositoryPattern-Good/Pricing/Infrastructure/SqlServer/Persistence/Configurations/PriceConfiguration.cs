using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;

namespace WebShop.Pricing.Infrastructure.SqlServer.Persistence.Configurations;

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

        entity.ComplexProperty(e => e.ShopPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("ShopPrice");
            price.Property(m => m.Currency).HasColumnName("ShopPriceCurrency").HasMaxLength(3).IsRequired();
        });

        entity.ComplexProperty(e => e.ShippingPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("ShippingPrice");
            price.Property(m => m.Currency).HasColumnName("ShippingPriceCurrency").HasMaxLength(3).IsRequired();
        });

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.ShopId);
        entity.HasIndex(e => new { e.ShopId, e.ProductId }).IsUnique();

        entity.HasOne<Shop>()
            .WithMany()
            .HasForeignKey(e => e.ShopId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
