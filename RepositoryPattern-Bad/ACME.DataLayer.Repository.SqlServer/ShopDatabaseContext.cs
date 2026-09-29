using ACME.DataLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace ACME.DataLayer.Repository.SqlServer;

public partial class ShopDatabaseContext : DbContext
{
    public ShopDatabaseContext(DbContextOptions<ShopDatabaseContext> options)
        : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ConsumerReview> ConsumerReviews => Set<ConsumerReview>();
    public DbSet<ExpertReview> ExpertReviews => Set<ExpertReview>();
    public DbSet<WebReview> WebReviews => Set<WebReview>();
    public DbSet<Reviewer> Reviewers => Set<Reviewer>();
    public DbSet<Specification> Specifications => Set<Specification>();
    public DbSet<SpecificationDefinition> SpecificationDefinitions => Set<SpecificationDefinition>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Core");
        modelBuilder.Entity<Product>().Navigation(p => p.Brand).AutoInclude();
        modelBuilder.Entity<Review>(opts => {
            opts.Navigation(r => r.Reviewer).AutoInclude();
        });

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.GetProperty(nameof(Entity.Timestamp)).ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
        }

        modelBuilder.Entity<Review>(conf =>
        {
            conf.HasDiscriminator(r => r.ReviewType)
                .HasValue<Review>(ReviewType.Generic)
                .HasValue<ConsumerReview>(ReviewType.Consumer)
                .HasValue<ExpertReview>(ReviewType.Expert)
                .HasValue<WebReview>(ReviewType.Web);
        });
    }
}
