using System.Net;
using Microsoft.Azure.Cosmos;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;

namespace WebShop.Catalog.Infrastructure.Cosmos.Persistence;

public sealed class ProductGroupRepository(CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker) : IProductGroupRepository
{
    private const string Type = "ProductGroup";
    private static readonly PartitionKey PartitionKeyValue = new(Type);

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Reference);

    public async Task<ProductGroup?> GetById(ProductGroupId id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<ProductGroupDocument>(
                ProductGroupDocument.BuildId(id.Value), PartitionKeyValue, cancellationToken: cancellationToken);
            return ProductGroupDocumentAdapter.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ProductGroup>> GetByParentId(ProductGroupId? parentId, CancellationToken cancellationToken)
    {
        var query = parentId is null
            ? new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND NOT IS_DEFINED(c.parentId) ORDER BY c.name")
                .WithParameter("@type", Type)
            : new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND c.parentId = @parentId ORDER BY c.name")
                .WithParameter("@type", Type)
                .WithParameter("@parentId", parentId.Value.Value);

        var results = new List<ProductGroup>();
        using var iterator = Container.GetItemQueryIterator<ProductGroupDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = PartitionKeyValue });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(ProductGroupDocumentAdapter.ToDomain));
        }

        return results;
    }

    public void Add(ProductGroup productGroup)
    {
        var document = ProductGroupDocumentAdapter.ToDocument(productGroup);
        changeTracker.StageUpsert(CosmosContainers.Reference, PartitionKeyValue, document.Id, document);
    }
}
