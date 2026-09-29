using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;

namespace WebShop.Reviews.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class ReviewUserConfiguration : IEntityTypeConfiguration<ReviewUser>
{
    public void Configure(EntityTypeBuilder<ReviewUser> entity)
    {
        entity.ToTable("ReviewUser");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ReviewUserId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(510).IsRequired();
        entity.Property(e => e.Email).HasMaxLength(320);

        entity.HasIndex(e => e.Name).IsUnique();
    }
}
