using Microsoft.EntityFrameworkCore;

namespace DemoTransactions;

internal class ProductHistoryContext : DbContext
{
    public ProductHistoryContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProcessedOutboxMessage> ProcessedOutboxMessages => Set<ProcessedOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Core");

        // Id mirrors the source OutboxMessage.Id - it's not a surrogate key, so don't let EF treat
        // it as an IDENTITY column (which would reject the explicit value we assign to it).
        modelBuilder.Entity<ProcessedOutboxMessage>().Property(p => p.Id).ValueGeneratedNever();
    }
}
