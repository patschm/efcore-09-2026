using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Search.Domain.Aggregates;

namespace WebShop.Search.Infrastructure.Postgres.Persistence.Contexts;

public class SearchPgContext : DbContext
{
    public SearchPgContext(DbContextOptions<SearchPgContext> options)
        : base(options)
    {
    }

    public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No default schema: ProductEmbeddings maps onto the real, pre-existing "public"."ProductEmbeddingQwen3"
        // table the old scaffolded app used - not a fresh code-first schema.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
