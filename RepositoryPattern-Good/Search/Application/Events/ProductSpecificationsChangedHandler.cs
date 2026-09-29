using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Contracts;
using WebShop.Search.Application.Commands;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Application.Events;

// The only place Search's Application layer knows Catalog's published contract exists. It maps
// the contract onto the same UpsertProductEmbeddingCommand a direct caller would use - no
// duplicated upsert logic - and, per the embeddings-are-spec-based decision, builds the
// embedding text from ProductGroupName + Specifications, ignoring anything else the event might
// carry later (in particular, never Product.Name or Brand - see the integration event's own
// comment for why). "{category}. {spec}; {spec}; ..." mirrors the original embedgen tool this
// replaces - see D:\BesteProduct\embedgen\Program.cs's contentSql.
public sealed class ProductSpecificationsChangedHandler(ICommandHandler<UpsertProductEmbeddingCommand> upsertEmbedding)
    : IIntegrationEventHandler<ProductSpecificationsChangedIntegrationEvent>
{
    public Task Handle(ProductSpecificationsChangedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var specifications = string.Join("; ", integrationEvent.Specifications.Select(s => $"{s.Name}: {s.DisplayValue}"));
        var content = $"{integrationEvent.ProductGroupName}. {specifications}";
        return upsertEmbedding.Handle(
            new UpsertProductEmbeddingCommand(new ProductId(integrationEvent.ProductId), content), cancellationToken);
    }
}
