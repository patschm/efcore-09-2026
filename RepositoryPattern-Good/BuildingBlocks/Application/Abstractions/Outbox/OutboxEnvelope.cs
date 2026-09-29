namespace WebShop.BuildingBlocks.Application.Abstractions.Outbox;

// The relay's view of a stored message: just enough to deserialize and dispatch it. It never
// needs the message's identity beyond marking it processed afterwards.
public sealed record OutboxEnvelope(Guid Id, string Content);
