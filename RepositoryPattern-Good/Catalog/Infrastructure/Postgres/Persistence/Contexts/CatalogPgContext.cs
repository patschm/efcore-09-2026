using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

public class CatalogPgContext : DbContext
{
    public CatalogPgContext(DbContextOptions<CatalogPgContext> options)
        : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<SpecificationDefinition> SpecificationDefinitions => Set<SpecificationDefinition>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSpecificationValue> ProductSpecificationValues => Set<ProductSpecificationValue>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No default schema: these tables are the real, pre-existing "public" schema tables the
        // old scaffolded app used (and real data lives in), not a fresh code-first schema - this
        // context maps onto them, it doesn't own or generate them. OutboxMessage is the one
        // genuinely new table here; see EfOutbox's schema for why it lives apart from "public".
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    // Postgres has no auto-incrementing rowversion type, so unlike SQL Server's `rowversion`,
    // RowVersion here is a plain bigint the database will not update on its own - bumping it on
    // every modified, concurrency-tracked entity is what makes the "WHERE RowVersion = @original"
    // EF generates for the UPDATE actually mean something instead of always matching.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void BumpRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
                continue;

            var rowVersion = entry.Metadata.FindProperty("RowVersion");
            if (rowVersion is not { IsConcurrencyToken: true })
                continue;

            var property = entry.Property(rowVersion.Name);
            property.CurrentValue = (long)property.CurrentValue! + 1;
        }
    }
}
