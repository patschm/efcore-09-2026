using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;

namespace WebShop.Catalog.Infrastructure.Cosmos;

// Postgres needs the outbox because nothing else notices a row changed until something polls for
// it (see EfOutbox/ProductSpecificationsChangedRelay). Cosmos doesn't have that problem: the
// change feed relay (Integration/CosmosChangeFeed) already watches the products container's raw
// SpecValue writes directly and re-triggers Search's embedding pipeline from that - there's
// nothing for an outbox row to add. SetProductSpecificationValueCommandHandler still calls
// IOutbox.Enqueue unconditionally (it's provider-agnostic), so this just no-ops it here.
public sealed class NullOutbox : IOutbox
{
    public void Enqueue(IIntegrationEvent integrationEvent)
    {
    }
}
