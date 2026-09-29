namespace WebShop.BuildingBlocks.Application.Abstractions.Outbox;

// Read side of the outbox, used by a relay rather than by command handlers. Kept separate from
// IOutbox because the two run in entirely different processes/lifetimes in a real deployment -
// a command handler enqueues within a request's unit of work, a relay polls independently.
public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxEnvelope>> GetPending(string messageType, CancellationToken cancellationToken);
    Task MarkProcessed(Guid messageId, DateTime processedOnUtc, CancellationToken cancellationToken);
}
