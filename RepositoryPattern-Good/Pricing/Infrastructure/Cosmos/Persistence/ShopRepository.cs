using System.Net;
using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Cosmos.Adapters;
using WebShop.Pricing.Infrastructure.Cosmos.Documents;

namespace WebShop.Pricing.Infrastructure.Cosmos.Persistence;

public sealed class ShopRepository(CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker) : IShopRepository
{
    private const string Type = "Shop";
    private static readonly PartitionKey PartitionKeyValue = new(Type);

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Reference);

    public async Task<Shop?> GetById(ShopId id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<ShopDocument>(ShopDocument.BuildId(id.Value), PartitionKeyValue, cancellationToken: cancellationToken);
            return ShopDocumentAdapter.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Shop>> GetByIds(IReadOnlyCollection<ShopId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var idValues = ids.Select(id => ShopDocument.BuildId(id.Value)).ToArray();
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND ARRAY_CONTAINS(@ids, c.id)")
            .WithParameter("@type", Type)
            .WithParameter("@ids", idValues);

        var results = new List<Shop>();
        using var iterator = Container.GetItemQueryIterator<ShopDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = PartitionKeyValue });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(ShopDocumentAdapter.ToDomain));
        }

        return results;
    }

    public void Add(Shop shop)
    {
        var document = ShopDocumentAdapter.ToDocument(shop);
        changeTracker.StageUpsert(CosmosContainers.Reference, PartitionKeyValue, document.Id, document);
    }
}
