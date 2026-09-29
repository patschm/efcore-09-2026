using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Events;

public sealed record ProductCreated(ProductId ProductId, DateTime OccurredOnUtc) : IDomainEvent;
