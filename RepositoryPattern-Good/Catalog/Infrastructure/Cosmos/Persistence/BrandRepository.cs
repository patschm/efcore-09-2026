using System.Net;
using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;

namespace WebShop.Catalog.Infrastructure.Cosmos.Persistence;

public sealed class BrandRepository(CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker) : IBrandRepository
{
    private const string Type = "Brand";
    private static readonly PartitionKey PartitionKeyValue = new(Type);

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Reference);

    public async Task<Brand?> GetById(BrandId id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<BrandDocument>(BrandDocument.BuildId(id.Value), PartitionKeyValue, cancellationToken: cancellationToken);
            return BrandDocumentAdapter.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Brand>> GetByIds(IReadOnlyCollection<BrandId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var idValues = ids.Select(id => BrandDocument.BuildId(id.Value)).ToArray();
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND ARRAY_CONTAINS(@ids, c.id)")
            .WithParameter("@type", Type)
            .WithParameter("@ids", idValues);

        var results = new List<Brand>();
        using var iterator = Container.GetItemQueryIterator<BrandDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = PartitionKeyValue });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(BrandDocumentAdapter.ToDomain));
        }

        return results;
    }

    public void Add(Brand brand)
    {
        var document = BrandDocumentAdapter.ToDocument(brand);
        changeTracker.StageUpsert(CosmosContainers.Reference, PartitionKeyValue, document.Id, document);
    }
}
