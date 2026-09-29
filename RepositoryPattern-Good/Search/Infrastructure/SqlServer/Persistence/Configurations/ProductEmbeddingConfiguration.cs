using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;

namespace WebShop.Search.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ProductEmbeddingConfiguration : IEntityTypeConfiguration<ProductEmbedding>
{
    public void Configure(EntityTypeBuilder<ProductEmbedding> entity)
    {
        entity.ToTable("ProductEmbeddings");
        entity.HasKey(e => e.ProductId);

        entity.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .ValueGeneratedNever();

        // SQL Server 2025's native VECTOR(1024) type - see ProductEmbeddingRepository for the
        // raw-SQL read/write path this requires. No FK to Catalog's Product table on purpose:
        // Search only knows a ProductId.
        entity.Property(e => e.Vector).HasColumnType("vector(1024)");

        entity.Property(e => e.ContentHash).HasColumnType("varbinary(32)");
        entity.Property(e => e.GeneratedAt);
    }
}
