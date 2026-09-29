using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Cosmos.Adapters;
using WebShop.Pricing.Infrastructure.Cosmos.Documents;

namespace WebShop.Pricing.Infrastructure.Cosmos.Persistence;

public sealed class PriceRepository(
    CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker, IShopRepository shopRepository) : IPriceRepository
{
    private const string Type = "Price";

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Products);

    // PriceId alone doesn't carry the productId partition key, so this is a cross-partition
    // query rather than a point read - an accepted cost of choosing productId (not priceId) as
    // the partition key, since the hot path (render a product's prices) needs GetByProductId
    // to be the cheap one, not this.
    public async Task<Price?> GetById(PriceId id, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND c.priceId = @priceId")
            .WithParameter("@type", Type)
            .WithParameter("@priceId", id.Value);

        using var iterator = Container.GetItemQueryIterator<PriceDocument>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            var document = page.FirstOrDefault();
            if (document is not null)
                return PriceDocumentAdapter.ToDomain(document);
        }

        return null;
    }

    public async Task<IReadOnlyList<Price>> GetByProductId(ProductId productId, CancellationToken cancellationToken)
    {
        var partitionKey = new PartitionKey(productId.Value);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type").WithParameter("@type", Type);

        var results = new List<Price>();
        using var iterator = Container.GetItemQueryIterator<PriceDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(PriceDocumentAdapter.ToDomain));
        }

        return results;
    }

    public async Task<IReadOnlyList<Price>> GetByProductIds(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND ARRAY_CONTAINS(@productIds, c.productId)")
            .WithParameter("@type", Type)
            .WithParameter("@productIds", productIds.Select(id => id.Value).ToArray());

        var results = new List<Price>();
        using var iterator = Container.GetItemQueryIterator<PriceDocument>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(PriceDocumentAdapter.ToDomain));
        }

        return results;
    }

    // Async Shop lookup (for the denormalized name/logo/rating snapshot) can't happen inside a
    // synchronous Add - staged as a factory and resolved once, during SaveChangesAsync's flush.
    public void Add(Price price) =>
        changeTracker.StageUpsertBatch(CosmosContainers.Products, new PartitionKey(price.ProductId.Value), async ct =>
        {
            var shop = await shopRepository.GetById(price.ShopId, ct);
            var document = PriceDocumentAdapter.ToDocument(price, shop);
            return (IReadOnlyList<(string ItemId, object Item)>) [(document.Id, document)];
        });
}
