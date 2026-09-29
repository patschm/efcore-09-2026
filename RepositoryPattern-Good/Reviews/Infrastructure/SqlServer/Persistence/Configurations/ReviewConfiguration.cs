using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;

namespace WebShop.Reviews.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> entity)
    {
        entity.ToTable("Review");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ReviewId(value))
            .ValueGeneratedNever();

        // ProductId is a reference into the Catalog bounded context - this project has
        // no dependency on WebShop.Catalog, so it's mapped as a plain converted column,
        // not a navigable relationship.
        entity.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .IsRequired();

        entity.Property(e => e.Type).HasMaxLength(20).IsRequired();
        entity.Property(e => e.Title).HasMaxLength(510);
        entity.Property(e => e.Score).HasPrecision(5, 2);

        entity.Property(e => e.ReviewUserId).HasConversion(
            id => id.HasValue ? id.Value.Value : (int?)null,
            value => value.HasValue ? new ReviewUserId(value.Value) : (ReviewUserId?)null);

        entity.HasIndex(e => e.ProductId);
        entity.HasIndex(e => e.Type);

        entity.HasOne<ReviewUser>()
            .WithMany()
            .HasForeignKey(e => e.ReviewUserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
