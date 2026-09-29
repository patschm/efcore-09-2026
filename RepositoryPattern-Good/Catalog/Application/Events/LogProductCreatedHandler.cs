using Microsoft.Extensions.Logging;
using WebShop.Catalog.Domain.Events;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Catalog.Application.Events;

// Stand-in handler proving the raise -> collect -> dispatch round trip end to end. The real
// motivating reaction (Search generating an embedding for the new product) crosses a bounded
// context boundary, which needs an integration event (e.g. via an outbox) rather than calling
// another context's handler directly - that's a separate, larger follow-up.
public sealed class LogProductCreatedHandler(ILogger<LogProductCreatedHandler> logger) : IDomainEventHandler<ProductCreated>
{
    public Task Handle(ProductCreated domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ProductCreated: productId={ProductId} at={OccurredOnUtc}",
            domainEvent.ProductId.Value, domainEvent.OccurredOnUtc);
        return Task.CompletedTask;
    }
}
