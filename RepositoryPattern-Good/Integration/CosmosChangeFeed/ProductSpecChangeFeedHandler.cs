using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Search.Application.Commands;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Integration.CosmosChangeFeed;

// Watches the "products" container's change feed for SpecValue changes and re-triggers Search's
// embedding pipeline - the Cosmos-side equivalent of Catalog's outbox ->
// ProductSpecificationsChangedIntegrationEvent -> ProductSpecificationsChangedHandler path (see
// Catalog/Application/Commands/SetProductSpecificationValueCommandHandler.cs), reusing the exact
// same UpsertProductEmbeddingCommandHandler at the end instead of re-deriving embedding-write
// logic. There's no outbox row to poll on the Cosmos side - the repositories (and
// Tools/CosmosMigration) write SpecValue documents directly, so the change feed itself is what
// has to notice the change.
//
// A product with many spec values touched in one write batch fires one change-feed event per
// SpecValue document, each re-running RebuildEmbeddingAsync for that product - wasteful (repeat
// Cosmos reads) but harmless: UpsertProductEmbeddingCommandHandler compares a content hash and
// skips the actual embedding model call once the first event in the batch already brought the
// embedding up to date.
public sealed class ProductSpecChangeFeedHandler(
    CosmosClient client, CosmosOptions options, UpsertProductEmbeddingCommandHandler upsertEmbedding)
{
    private Container Products => client.GetContainer(options.DatabaseName, CosmosContainers.Products);
    private Container Reference => client.GetContainer(options.DatabaseName, CosmosContainers.Reference);

    public async Task HandleChangesAsync(IReadOnlyCollection<JObject> changes, CancellationToken cancellationToken)
    {
        var productIds = changes
            .Where(change => (string?)change["type"] == "SpecValue")
            .Select(change => (int)change["productId"]!)
            .Distinct();

        foreach (var productId in productIds)
            await RebuildEmbeddingAsync(productId, cancellationToken);
    }

    private async Task RebuildEmbeddingAsync(int productId, CancellationToken cancellationToken)
    {
        JObject product;
        try
        {
            var response = await Products.ReadItemAsync<JObject>(
                $"product|{productId}", new PartitionKey(productId), cancellationToken: cancellationToken);
            product = response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        // Matches Postgres's BuildSpecificationSnapshot: no group, no snapshot, nothing to embed.
        if ((int?)product["productGroupId"] is not { } groupId)
            return;

        string groupName;
        try
        {
            var groupResponse = await Reference.ReadItemAsync<JObject>(
                groupId.ToString(), new PartitionKey("ProductGroup"), cancellationToken: cancellationToken);
            groupName = (string)groupResponse.Resource["name"]!;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        var specifications = new List<string>();
        var query = new QueryDefinition("SELECT c.name, c.numberValue, c.boolValue, c.stringValue FROM c WHERE c.type = 'SpecValue'");
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(productId) };
        using var iterator = Products.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
            {
                // Free-text values are excluded, mirroring the Postgres path's Describe() - noisy/
                // inconsistent, and they pollute similarity search (see
                // SetProductSpecificationValueCommandHandler's comment on why).
                if (row["stringValue"] is not null)
                    continue;

                var display = row["numberValue"]?.ToString() ?? row["boolValue"]?.ToString() ?? string.Empty;
                specifications.Add($"{row["name"]}: {display}");
            }
        }

        var content = $"{groupName}. {string.Join("; ", specifications)}";
        await upsertEmbedding.Handle(new UpsertProductEmbeddingCommand(new ProductId(productId), content), cancellationToken);
    }
}
