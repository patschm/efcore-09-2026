namespace WebShop.SharedKernel;

// Opt-in base for aggregates that need to raise domain events - not every aggregate needs
// this, only ones with something worth telling the rest of the context (or, via an
// integration event, another bounded context) about.
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
