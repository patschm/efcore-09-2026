namespace WebShop.BuildingBlocks.Application.Abstractions.Messaging;

// Cross-bounded-context sibling of IDomainEvent: a domain event stays inside the context that
// raised it, an integration event is the "published language" another context subscribes to.
public interface IIntegrationEvent
{
    DateTime OccurredOnUtc { get; }
}
