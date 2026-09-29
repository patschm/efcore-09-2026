using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace WebShop.BuildingBlocks.Cosmos;

// Idempotent container setup shared by every context's Cosmos infrastructure. Whichever
// context's DI extension runs first "wins" and provisions both containers; the others are
// no-ops against an already-provisioned database, since CreateContainerIfNotExistsAsync is
// idempotent - mirrors the existing EnsureOutboxSchemaCreatedAsync() convention for Postgres.
public static class CosmosContainerProvisioner
{
    // containerThroughput is null for the emulator/local dev (relies on whatever default the
    // emulator gives an unthrottled container) and set to an autoscale ThroughputProperties for
    // a real provisioned-mode Azure account, which has no free/serverless default to fall back on.
    public static async Task EnsureContainersCreatedAsync(
        CosmosClient client, string databaseName, CancellationToken cancellationToken = default, ThroughputProperties? containerThroughput = null)
    {
        var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: cancellationToken);
        var database = databaseResponse.Database;

        await database.CreateContainerIfNotExistsAsync(BuildReferenceContainerProperties(), containerThroughput, cancellationToken: cancellationToken);
        await database.CreateContainerIfNotExistsAsync(BuildProductsContainerProperties(), containerThroughput, cancellationToken: cancellationToken);
    }

    // Not part of EnsureContainersCreatedAsync above - a lease container is only needed by
    // whichever process actually runs a ChangeFeedProcessor (see
    // Integration/CosmosChangeFeed), not by every context's DI registration. Manual (not
    // autoscale) throughput at the lowest tier: leases are tiny, low-write documents (one per
    // physical partition being monitored), nowhere near needing autoscale's burst headroom.
    public static async Task EnsureChangeFeedLeaseContainerCreatedAsync(
        CosmosClient client, string databaseName, string leaseContainerName = "leases", CancellationToken cancellationToken = default)
    {
        var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: cancellationToken);
        await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(leaseContainerName, "/id"), throughput: 400, cancellationToken: cancellationToken);
    }

    private static ContainerProperties BuildReferenceContainerProperties() =>
        new(CosmosContainers.Reference, "/type")
        {
            IndexingPolicy = new IndexingPolicy
            {
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                CompositeIndexes =
                {
                    new Collection<CompositePath>
                    {
                        new() { Path = "/type", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/parentId", Order = CompositePathSortOrder.Ascending }
                    }
                }
            }
        };

    private static ContainerProperties BuildProductsContainerProperties() =>
        new(CosmosContainers.Products, "/productId")
        {
            // TTL mechanism on, off per item unless an item sets its own "ttl" - only soft-deleted
            // Review items use it today (see SoftDelete). Every other item type is unaffected.
            DefaultTimeToLive = -1,
            VectorEmbeddingPolicy = new VectorEmbeddingPolicy(new Collection<Embedding>
            {
                new()
                {
                    Path = "/vector",
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 1024
                }
            }),
            IndexingPolicy = new IndexingPolicy
            {
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                // Excluded from the default range index and given a dedicated vector index instead -
                // indexing a 1024-float array the normal way would be expensive and pointless.
                ExcludedPaths = { new ExcludedPath { Path = "/vector/*" } },
                VectorIndexes = { new VectorIndexPath { Path = "/vector", Type = VectorIndexType.QuantizedFlat } },
                CompositeIndexes =
                {
                    // Keeps the cross-partition "browse by category/brand" queries affordable -
                    // they can never be as cheap as the single-partition product-detail read,
                    // but a composite index keeps them from being a full unindexed scan.
                    new Collection<CompositePath>
                    {
                        new() { Path = "/type", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/productGroupId", Order = CompositePathSortOrder.Ascending }
                    },
                    new Collection<CompositePath>
                    {
                        new() { Path = "/type", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/brandId", Order = CompositePathSortOrder.Ascending }
                    }
                }
            }
        };
}
