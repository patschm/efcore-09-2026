namespace WebShop.BuildingBlocks.Application.Abstractions.Messaging;

// Mirrors IDomainEventHandler: the consuming context depends on the specific handler it needs
// and calls Handle directly - no generic dispatcher, no runtime type lookup.
public interface IIntegrationEventHandler<in TIntegrationEvent> where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
