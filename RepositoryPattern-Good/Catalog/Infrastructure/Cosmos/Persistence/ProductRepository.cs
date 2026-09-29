using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;

namespace WebShop.Catalog.Infrastructure.Cosmos.Persistence;

public sealed class ProductRepository(
    CosmosClient client,
    CosmosOptions options,
    CosmosChangeTracker changeTracker,
    IBrandRepository brandRepository,
    IProductGroupRepository productGroupRepository) : IProductRepository
{
    private const string ProductType = "Product";
    private const string SpecValueType = "SpecValue";

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Products);

    public async Task<Product?> GetById(ProductId id, CancellationToken cancellationToken)
    {
        var partitionKey = new PartitionKey(id.Value);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.productId = @productId AND (c.type = @productType OR c.type = @specValueType)")
            .WithParameter("@productId", id.Value)
            .WithParameter("@productType", ProductType)
            .WithParameter("@specValueType", SpecValueType);

        ProductDocument? productDocument = null;
        var specValueDocuments = new List<SpecValueDocument>();

        using var iterator = Container.GetItemQueryIterator<JObject>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in page)
            {
                if (item.Value<string>("type") == ProductType)
                    productDocument = item.ToObject<ProductDocument>();
                else
                    specValueDocuments.Add(item.ToObject<SpecValueDocument>()!);
            }
        }

        return productDocument is null ? null : ProductDocumentAdapter.ToDomain(productDocument, specValueDocuments);
    }

    public async Task<IReadOnlyList<Product>> GetByProductGroupId(ProductGroupId productGroupId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type AND c.productGroupId = @productGroupId ORDER BY c.productId DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@type", ProductType)
            .WithParameter("@productGroupId", productGroupId.Value)
            .WithParameter("@offset", (page - 1) * pageSize)
            .WithParameter("@limit", pageSize);

        return await QueryProducts(query, cancellationToken);
    }

    public async Task<int> CountByProductGroupId(ProductGroupId productGroupId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = @type AND c.productGroupId = @productGroupId")
            .WithParameter("@type", ProductType)
            .WithParameter("@productGroupId", productGroupId.Value);

        using var iterator = Container.GetItemQueryIterator<int>(query);
        var page = await iterator.ReadNextAsync(cancellationToken);
        return page.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Product>> GetByIds(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND ARRAY_CONTAINS(@productIds, c.productId)")
            .WithParameter("@type", ProductType)
            .WithParameter("@productIds", ids.Select(id => id.Value).ToArray());

        return await QueryProducts(query, cancellationToken);
    }

    private async Task<IReadOnlyList<Product>> QueryProducts(QueryDefinition query, CancellationToken cancellationToken)
    {
        // No SpecificationValues for these list-style reads - matches the Postgres/SqlServer
        // repositories, which don't Include() them here either.
        var results = new List<Product>();
        using var iterator = Container.GetItemQueryIterator<ProductDocument>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(d => ProductDocumentAdapter.ToDomain(d, [])));
        }

        return results;
    }

    // Add can't await anything (the interface requires a synchronous void), but building this
    // document needs async lookups (brand name, category breadcrumb, the group's specification
    // definitions) - so the lookups are staged as a factory and only run once, during
    // CosmosUnitOfWork.SaveChangesAsync's flush. Product doc + all its SpecValue docs share the
    // product's partition, so they land in one atomic TransactionalBatch.
    public void Add(Product product) =>
        changeTracker.StageUpsertBatch(CosmosContainers.Products, new PartitionKey(product.Id.Value), ct => BuildItemsAsync(product, ct));

    private async Task<IReadOnlyList<(string ItemId, object Item)>> BuildItemsAsync(Product product, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetById(product.BrandId, cancellationToken);

        var groupPath = new List<GroupPathEntry>();
        var definitionsById = new Dictionary<SpecificationDefinitionId, SpecificationDefinition>();

        if (product.ProductGroupId is { } leafGroupId)
        {
            var leafGroup = await productGroupRepository.GetById(leafGroupId, cancellationToken);
            if (leafGroup is not null)
            {
                foreach (var definition in leafGroup.SpecificationDefinitions)
                    definitionsById[definition.Id] = definition;

                var chain = new List<ProductGroup> { leafGroup };
                var current = leafGroup;
                while (current.ParentId is { } parentId)
                {
                    var parent = await productGroupRepository.GetById(parentId, cancellationToken);
                    if (parent is null)
                        break;

                    chain.Add(parent);
                    current = parent;
                }

                chain.Reverse();
                groupPath.AddRange(chain.Select(g => new GroupPathEntry { Id = g.Id.Value, Name = g.Name }));
            }
        }

        var productDocument = ProductDocumentAdapter.ToDocument(product, brand?.Name, groupPath);
        var specValueDocuments = ProductDocumentAdapter.ToSpecValueDocuments(product, definitionsById);

        var items = new List<(string, object)> { (productDocument.Id, productDocument) };
        items.AddRange(specValueDocuments.Select(d => (d.Id, (object)d)));
        return items;
    }
}
