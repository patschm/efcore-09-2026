using System.Text.Json;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.Catalog.Contracts;

namespace WebShop.Integration.CatalogSearch;

// Stands in for a message broker: for now, "publishing" means polling Catalog's outbox and
// calling the subscriber's handler in-process. Depends only on the two contracts either side
// already has to have (IOutboxReader, IIntegrationEventHandler<T>) - it never references
// Catalog's or Search's own Domain/Application/Infrastructure projects, so swapping this for a
// real broker later touches only this project, not either context.
public sealed class ProductSpecificationsChangedRelay(
    IOutboxReader outboxReader,
    IIntegrationEventHandler<ProductSpecificationsChangedIntegrationEvent> handler)
{
    private const string MessageType = nameof(ProductSpecificationsChangedIntegrationEvent);

    public async Task<int> RelayPending(CancellationToken cancellationToken)
    {
        var pending = await outboxReader.GetPending(MessageType, cancellationToken);

        foreach (var message in pending)
        {
            var integrationEvent = JsonSerializer.Deserialize<ProductSpecificationsChangedIntegrationEvent>(message.Content)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} could not be deserialized as {MessageType}.");

            await handler.Handle(integrationEvent, cancellationToken);
            await outboxReader.MarkProcessed(message.Id, DateTime.UtcNow, cancellationToken);
        }

        return pending.Count;
    }
}
