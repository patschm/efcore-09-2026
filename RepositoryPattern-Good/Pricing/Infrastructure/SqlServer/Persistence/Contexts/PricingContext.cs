using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Aggregates;

namespace WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;

public class PricingContext : DbContext
{
    public PricingContext(DbContextOptions<PricingContext> options)
        : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Price> Prices => Set<Price>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
