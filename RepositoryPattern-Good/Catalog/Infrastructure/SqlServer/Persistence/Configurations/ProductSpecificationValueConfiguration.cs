using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ProductSpecificationValueConfiguration : IEntityTypeConfiguration<ProductSpecificationValue>
{
    public void Configure(EntityTypeBuilder<ProductSpecificationValue> entity)
    {
        entity.ToTable("ProductSpecificationValue");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ProductSpecificationValueId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .IsRequired();

        entity.Property(e => e.SpecificationDefinitionId)
            .HasConversion(id => id.Value, value => new SpecificationDefinitionId(value))
            .IsRequired();

        entity.ComplexProperty(e => e.Value, value =>
        {
            value.Property(v => v.Number).HasColumnName("NumberValue").HasPrecision(18, 8);
            value.Property(v => v.Text).HasColumnName("StringValue");
            value.Property(v => v.Flag).HasColumnName("BoolValue");
        });

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.SpecificationDefinitionId);

        entity.HasOne<SpecificationDefinition>()
            .WithMany()
            .HasForeignKey(e => e.SpecificationDefinitionId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
