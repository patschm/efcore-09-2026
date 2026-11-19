using Microsoft.EntityFrameworkCore;

namespace DemoEntityFramework
{
    internal class ProductContext : DbContext
    {
        public ProductContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Brand> Brands => Set<Brand>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           modelBuilder.HasDefaultSchema("Core");
        }
    }
}
