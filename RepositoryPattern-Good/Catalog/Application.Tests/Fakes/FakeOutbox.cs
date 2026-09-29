using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;

namespace WebShop.Catalog.Application.Tests.Fakes;

internal sealed class FakeOutbox : IOutbox
{
    public List<IIntegrationEvent> EnqueuedEvents { get; } = [];

    public void Enqueue(IIntegrationEvent integrationEvent) => EnqueuedEvents.Add(integrationEvent);
}
