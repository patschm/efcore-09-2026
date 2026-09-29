using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Outbox;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;

public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options)
        : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<SpecificationDefinition> SpecificationDefinitions => Set<SpecificationDefinition>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSpecificationValue> ProductSpecificationValues => Set<ProductSpecificationValue>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
