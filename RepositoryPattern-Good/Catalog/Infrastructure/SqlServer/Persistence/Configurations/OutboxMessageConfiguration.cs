using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Outbox;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.ToTable("OutboxMessage");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Type).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Content).IsRequired();
    }
}
