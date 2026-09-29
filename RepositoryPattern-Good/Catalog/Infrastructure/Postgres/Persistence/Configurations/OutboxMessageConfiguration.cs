using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        // Own schema, apart from "public" - this table is genuinely new (no legacy counterpart),
        // and keeping it out of "public" makes that visually obvious rather than looking like
        // one more table the old scaffolded app already knew about.
        entity.ToTable("OutboxMessage", "integration");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Type).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Content).IsRequired();
    }
}
