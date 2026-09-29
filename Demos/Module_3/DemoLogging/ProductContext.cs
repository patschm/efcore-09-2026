using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DemoLogging;

internal class ProductContext : DbContext
{
    private readonly ILogger<ProductContext> _logger;
    public ProductContext(DbContextOptions options, ILogger<ProductContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _logger.LogInformation("Starting OnModelCreating....");
        modelBuilder.HasDefaultSchema("Core");
        _logger.LogInformation("Ending OnModelCreating....");
    }
}
