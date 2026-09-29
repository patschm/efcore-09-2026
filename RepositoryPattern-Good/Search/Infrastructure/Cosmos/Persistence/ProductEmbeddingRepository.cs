using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Models;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.Cosmos.Adapters;
using WebShop.Search.Infrastructure.Cosmos.Documents;

namespace WebShop.Search.Infrastructure.Cosmos.Persistence;

public sealed class ProductEmbeddingRepository(CosmosClient client, CosmosOptions options) : IProductEmbeddingRepository
{
    private const string Type = "Embedding";

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Products);

    public async Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<EmbeddingDocument>(
                EmbeddingDocument.BuildId(productId.Value), new PartitionKey(productId.Value), cancellationToken: cancellationToken);
            return EmbeddingDocumentAdapter.ToDomain(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // Writes immediately, same as the Postgres/SqlServer providers (Upsert bypasses IUnitOfWork
    // by design - see IProductEmbeddingRepository) - but for a different reason. Those providers
    // need raw SQL because a native vector column can't round-trip through normal SaveChanges;
    // Cosmos has no such limitation (a vector is just a JSON array here), so there's nothing to
    // defer through CosmosChangeTracker either - this can just write straight through.
    public Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken)
    {
        var document = EmbeddingDocumentAdapter.ToDocument(embedding);
        return Container.UpsertItemAsync(document, new PartitionKey(embedding.ProductId.Value), cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken)
    {
        // Cross-partition by nature - similarity search means scanning the whole corpus of
        // embeddings, not one product's partition, unlike every other query in this design.
        var query = new QueryDefinition(
                "SELECT c.productId, VectorDistance(c.vector, @vector) AS score FROM c " +
                "WHERE c.type = @type AND (IS_NULL(@exclude) OR c.productId != @exclude) " +
                "ORDER BY VectorDistance(c.vector, @vector) OFFSET 0 LIMIT @topN")
            .WithParameter("@vector", EmbeddingVectorFormat.Parse(vector))
            .WithParameter("@type", Type)
            .WithParameter("@exclude", (object?)exclude?.Value)
            .WithParameter("@topN", topN);

        var results = new List<SimilarProduct>();
        using var iterator = Container.GetItemQueryIterator<SimilarityRow>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(r => new SimilarProduct(new ProductId(r.ProductId), r.Score)));
        }

        return results;
    }

    private sealed class SimilarityRow
    {
        [JsonProperty("productId")] public int ProductId { get; set; }
        [JsonProperty("score")] public double Score { get; set; }
    }
}
