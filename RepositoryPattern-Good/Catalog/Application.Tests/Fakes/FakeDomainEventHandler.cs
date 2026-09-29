using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Application.Tests.Fakes;

internal sealed class FakeDomainEventHandler<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public List<TEvent> HandledEvents { get; } = [];

    public Task Handle(TEvent domainEvent, CancellationToken cancellationToken)
    {
        HandledEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
