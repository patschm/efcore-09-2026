using Microsoft.Extensions.Logging;
using WebShop.Catalog.Domain.Events;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Catalog.Application.Events;

// Stand-in, same role as LogProductCreatedHandler: proves the round trip. The real reaction
// (Search regenerating the product's embedding, built from its specification values - not its
// brand or name) needs the same cross-context integration-event follow-up as ProductCreated.
public sealed class LogProductSpecificationValueSetHandler(ILogger<LogProductSpecificationValueSetHandler> logger)
    : IDomainEventHandler<ProductSpecificationValueSet>
{
    public Task Handle(ProductSpecificationValueSet domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "ProductSpecificationValueSet: productId={ProductId} at={OccurredOnUtc}",
            domainEvent.ProductId.Value, domainEvent.OccurredOnUtc);
        return Task.CompletedTask;
    }
}
