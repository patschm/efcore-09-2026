using Microsoft.EntityFrameworkCore;

namespace DemoManipulation;

internal class ProductContext : DbContext
{
    public ProductContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>(); 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Core");
        modelBuilder.Entity<Brand>()
            .HasMany(b => b.Products)
            .WithOne(b => b.Brand)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<Brand>()
            .Property(b => b.Name)
            .IsConcurrencyToken();

        // DeleteBehavior, using this Brand (principal) / Product (dependent, FK Product.BrandId) relationship:
        //
        //   Value           | Tracked dependents in memory          | Database (Migrations/EnsureCreated)
        //   ----------------|----------------------------------------|---------------------------------------
        //   Cascade         | Products deleted with the Brand        | ON DELETE CASCADE (DB cascades too)
        //   ClientCascade   | Products deleted with the Brand        | ON DELETE NO ACTION (DB does nothing)
        //   SetNull         | Product.BrandId set to null            | ON DELETE SET NULL (DB nulls too)
        //   ClientSetNull   | Product.BrandId set to null            | ON DELETE NO ACTION (DB does nothing)
        //   Restrict        | Remove() throws immediately            | ON DELETE NO ACTION
        //   NoAction        | Nothing changes; BrandId now dangles   | ON DELETE NO ACTION
        //   ClientNoAction  | Same as NoAction (legacy/EF6-compat)   | ON DELETE NO ACTION
        //
        // "Tracked dependents" only applies to Products that are currently loaded into this DbContext (e.g. via
        // .Include()). Untracked dependent rows are only ever handled by the database column, which is why the
        // "Client*" variants (used below) can behave completely differently in-memory vs. from raw SQL: EF can
        // cascade/null out loaded Products, but a direct SQL DELETE on the Brand row hits ON DELETE NO ACTION and
        // fails with a foreign key violation, since the DB was never told to cascade or null on its own.
        // ClientSetNull is the default for optional (nullable FK) relationships; Cascade is the default for
        // required (non-nullable FK) relationships, which is what Product.BrandId is here. ClientCascade below
        // is a deliberate override of that default so the DB itself doesn't auto-cascade.
    }
}
