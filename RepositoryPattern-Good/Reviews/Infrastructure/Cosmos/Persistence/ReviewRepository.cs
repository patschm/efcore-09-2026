using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Cosmos.Adapters;
using WebShop.Reviews.Infrastructure.Cosmos.Documents;

namespace WebShop.Reviews.Infrastructure.Cosmos.Persistence;

public sealed class ReviewRepository(
    CosmosClient client, CosmosOptions options, CosmosChangeTracker changeTracker, IReviewUserRepository reviewUserRepository) : IReviewRepository
{
    private const string Type = "Review";

    // A grace period long enough that the (not-yet-built) fan-out/relay consumers have time to
    // react to the soft-delete before the item's TTL actually purges it - see SoftDelete.
    private static readonly TimeSpan SoftDeleteGracePeriod = TimeSpan.FromDays(7);

    private Container Container => client.GetContainer(options.DatabaseName, CosmosContainers.Products);

    // ReviewId alone doesn't carry the productId partition key, so this is a cross-partition
    // query rather than a point read - same trade-off as PriceRepository.GetById(PriceId).
    public async Task<Review?> GetById(ReviewId id, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND c.reviewId = @reviewId")
            .WithParameter("@type", Type)
            .WithParameter("@reviewId", id.Value);

        using var iterator = Container.GetItemQueryIterator<ReviewDocument>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            var document = page.FirstOrDefault();
            if (document is not null)
                return ReviewDocumentAdapter.ToDomain(document);
        }

        return null;
    }

    public async Task<IReadOnlyList<Review>> GetByProductId(ProductId productId, CancellationToken cancellationToken)
    {
        var partitionKey = new PartitionKey(productId.Value);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type AND c.isDeleted = false").WithParameter("@type", Type);

        var results = new List<Review>();
        using var iterator = Container.GetItemQueryIterator<ReviewDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Select(ReviewDocumentAdapter.ToDomain));
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<ProductId, double>> GetAverageScoresByProductIds(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<ProductId, double>();

        var query = new QueryDefinition(
                "SELECT c.productId, AVG(c.score) AS avgScore FROM c " +
                "WHERE c.type = @type AND c.isDeleted = false AND IS_DEFINED(c.score) AND ARRAY_CONTAINS(@productIds, c.productId) " +
                "GROUP BY c.productId")
            .WithParameter("@type", Type)
            .WithParameter("@productIds", productIds.Select(id => id.Value).ToArray());

        var results = new Dictionary<ProductId, double>();
        using var iterator = Container.GetItemQueryIterator<AverageScoreRow>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
                results[new ProductId(row.ProductId)] = row.AvgScore;
        }

        return results;
    }

    // Async ReviewUser lookup (for the denormalized reviewer snapshot) can't happen inside a
    // synchronous Add - staged as a factory and resolved once, during SaveChangesAsync's flush.
    public void Add(Review review) =>
        changeTracker.StageUpsertBatch(CosmosContainers.Products, new PartitionKey(review.ProductId.Value), async ct =>
        {
            var reviewUser = review.ReviewUserId is { } reviewUserId ? await reviewUserRepository.GetById(reviewUserId, ct) : null;
            var document = ReviewDocumentAdapter.ToDocument(review, reviewUser);
            return (IReadOnlyList<(string ItemId, object Item)>) [(document.Id, document)];
        });

    // Real deletes are invisible to Cosmos's classic change feed - patched to isDeleted/deletedAt
    // (an ordinary update, so it IS visible) with a ttl that purges the item later. See SoftDelete.
    public void Remove(Review review) =>
        changeTracker.StagePatch(
            CosmosContainers.Products,
            new PartitionKey(review.ProductId.Value),
            ReviewDocument.BuildId(review.Id.Value),
            SoftDelete.Patch(DateTime.UtcNow, SoftDeleteGracePeriod));

    private sealed class AverageScoreRow
    {
        [JsonProperty("productId")] public int ProductId { get; set; }
        [JsonProperty("avgScore")] public double AvgScore { get; set; }
    }
}
