using Microsoft.EntityFrameworkCore;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

public sealed class EfOutboxReader(CatalogPgContext context) : IOutboxReader
{
    public async Task<IReadOnlyList<OutboxEnvelope>> GetPending(string messageType, CancellationToken cancellationToken) =>
        await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.Type == messageType)
            .OrderBy(m => m.OccurredOnUtc)
            .Select(m => new OutboxEnvelope(m.Id, m.Content))
            .ToListAsync(cancellationToken);

    public async Task MarkProcessed(Guid messageId, DateTime processedOnUtc, CancellationToken cancellationToken)
    {
        var message = await context.Set<OutboxMessage>().SingleAsync(m => m.Id == messageId, cancellationToken);
        message.MarkProcessed(processedOnUtc);
        await context.SaveChangesAsync(cancellationToken);
    }
}
