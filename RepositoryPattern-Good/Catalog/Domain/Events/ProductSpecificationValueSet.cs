using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Events;

// Search's embedding content is built from a product's specification values, not its brand or
// name - this is what should eventually trigger Search's UpsertProductEmbeddingCommand, once
// the cross-context integration (outbox) piece connecting Catalog to Search is built.
public sealed record ProductSpecificationValueSet(ProductId ProductId, DateTime OccurredOnUtc) : IDomainEvent;
