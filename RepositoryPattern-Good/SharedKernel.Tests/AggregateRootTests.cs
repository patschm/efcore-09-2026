namespace WebShop.SharedKernel.Tests;

public class AggregateRootTests
{
    [Fact]
    public void Raise_adds_the_event_to_DomainEvents()
    {
        var aggregate = new TestAggregate();

        aggregate.RaiseTestEvent();

        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_empties_the_collection()
    {
        var aggregate = new TestAggregate();
        aggregate.RaiseTestEvent();

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void RaiseTestEvent() => Raise(new TestEvent());
    }

    private sealed record TestEvent : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }
}
