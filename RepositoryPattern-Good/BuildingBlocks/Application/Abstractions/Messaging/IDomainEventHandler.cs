using WebShop.SharedKernel;

namespace WebShop.BuildingBlocks.Application.Abstractions.Messaging;

// Mirrors ICommandHandler/IQueryHandler: callers depend on the specific handler they need for
// a given event and call Handle directly after SaveChangesAsync - no generic dispatcher, no
// runtime handler lookup, no pipeline behaviors.
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}
