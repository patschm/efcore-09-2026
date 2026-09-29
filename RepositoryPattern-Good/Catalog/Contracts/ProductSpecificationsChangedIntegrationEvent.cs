using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Catalog.Contracts;

// ProductId is a plain int, not Catalog's ProductId value type - subscribers outside Catalog
// never take a dependency on Catalog's own identifiers. Carries the full current snapshot of
// specification values (not just the one that changed) so a subscriber can rebuild its own
// projection from this message alone, without calling back into Catalog.
//
// ProductGroupName travels alongside Specifications deliberately: it's category context (e.g.
// "Televisions"), not brand or product-line naming, so it doesn't cause the same brand-clustering
// problem Product.Name/Brand did - see the embedding-content decision this feeds into.
public sealed record ProductSpecificationsChangedIntegrationEvent(
    int ProductId,
    string ProductGroupName,
    IReadOnlyList<SpecificationSnapshot> Specifications,
    DateTime OccurredOnUtc) : IIntegrationEvent;
