using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;

namespace WebShop.Search.Infrastructure.Postgres.Persistence.Configurations;

public sealed class ProductEmbeddingConfiguration : IEntityTypeConfiguration<ProductEmbedding>
{
    public void Configure(EntityTypeBuilder<ProductEmbedding> entity)
    {
        entity.ToTable("ProductEmbeddingQwen3");
        entity.HasKey(e => e.ProductId);

        entity.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .ValueGeneratedNever();

        // Native pgvector column (extension enabled in SearchPgContext) - reads/writes must go
        // through raw SQL with an explicit CAST, same limitation documented on ProductEmbedding.
        // No FK to Catalog's Product table on purpose: Search only knows a ProductId. Column is
        // "Embedding" on the real table, not "Vector" - HasColumnName reconciles the two names.
        entity.Property(e => e.Vector).HasColumnName("Embedding").HasColumnType("vector(1024)");

        entity.Property(e => e.ContentHash).HasColumnType("bytea");
        entity.Property(e => e.GeneratedAt);
    }
}
