namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;

// A plain persistence record, not a domain aggregate - it carries no business behavior beyond
// "has this been relayed yet", which is a technical concern of the outbox pattern itself.
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public void MarkProcessed(DateTime processedOnUtc) => ProcessedOnUtc = processedOnUtc;
}
