namespace WebShop.SharedKernel;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
