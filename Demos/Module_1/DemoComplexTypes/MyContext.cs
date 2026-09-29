using Microsoft.EntityFrameworkCore;

namespace DemoComplexTypes;

internal class MyContext : DbContext
{
    public MyContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Core");

        modelBuilder.Entity<Reviewer>(conf =>
        {
            // Complex types were introduced in EF8 with several limitations (no collections, no
            // nulls, no JSON mapping, no constructor binding, no seed data, no Cosmos/InMemory
            // support). Most of that was lifted by EF9/EF10. Credentials is nullable by default
            // now (optional complex property).
            conf.ComplexProperty(e => e.Credentials).IsRequired();

            // Primitive collection properties
            conf.Property(e => e.AssignedNumbers).HasMaxLength(100);
        });
       
    }
}
