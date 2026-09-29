using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Aggregates;

namespace WebShop.Pricing.Infrastructure.Postgres.Persistence.Contexts;

public class PricingPgContext : DbContext
{
    public PricingPgContext(DbContextOptions<PricingPgContext> options)
        : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Price> Prices => Set<Price>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No default schema: these are the real, pre-existing "public" schema tables the old
        // scaffolded app used, not a fresh code-first schema - this context maps onto them.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    // See CatalogPgContext.BumpRowVersions for why Postgres needs this bumped by hand.
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
