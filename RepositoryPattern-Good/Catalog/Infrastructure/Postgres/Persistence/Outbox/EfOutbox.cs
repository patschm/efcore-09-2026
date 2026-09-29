using System.Text.Json;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

public sealed class EfOutbox(CatalogPgContext context) : IOutbox
{
    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            integrationEvent.GetType().Name,
            JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            integrationEvent.OccurredOnUtc);

        context.Set<OutboxMessage>().Add(message);
    }
}
