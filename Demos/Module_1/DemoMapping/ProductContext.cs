using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DemoMapping;

internal class ProductContext : DbContext
{
    public ProductContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Produkt> Produkten => Set<Produkt>();
    public DbSet<Merk> Merken => Set<Merk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Core");

        modelBuilder.Entity<Merk>(conf =>
        {
            conf.ToTable("Brands");
            conf.HasKey("Id");
            //conf.Property(x=>x.Id).HasConversion<BrandIdConverter>(); // See ConfigureConventions
            conf.Property(x => x.Naam).HasColumnName("Name");
            conf.Property(x=>x.Timestamp).IsRowVersion().IsConcurrencyToken();
            conf.HasMany(b => b.Produkten)
                .WithOne(p => p.Merk)
                .HasForeignKey(p=>p.MerkId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Produkt>(conf =>
        {
            conf.ToTable("Products");
            conf.HasKey("Id");
            conf.Property(x => x.Naam).HasColumnName("Name");
            conf.Property(x => x.Afbeelding).HasColumnName("Image");
            //conf.Property(x => x.MerkId).HasColumnName("BrandId");
            //conf.Property(x=>x.MerkId).HasColumnName("BrandId").HasConversion<BrandIdConverter>();
            conf.Property(x => x.Timestamp).IsRowVersion().IsConcurrencyToken();
        });
#if false
        // Note: Many settings only apply to Code First approach
        modelBuilder.Entity<Merk>(conf => {
            // value generation with database function
            conf.Property("Created").HasDefaultValueSql("sql_function()");
            // Default Value
            conf.Property("Created").HasDefaultValue(DateTime.Now);
            // Overriding value generation
            conf.Property("Created").Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);
            // Computed Column
            conf.Property("FullName").HasComputedColumnSql("([Id]) + ' ' + [Name]");
            conf.Property("Name").HasMaxLength(255);
            // Primary Key wit multipe fields
            conf.HasKey(b => new { b.Id, b.Naam });
            // Define Index
            conf.HasIndex(b=>b.Naam).IsUnique().HasDatabaseName("idx_name");
            // Constraints
            conf.HasCheckConstraint("constraint_check", "[Id] > 0", b => b.HasName("CK_Id"));
        });
#endif
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<BrandId>().HaveConversion<BrandIdConverter>();
    }
}
