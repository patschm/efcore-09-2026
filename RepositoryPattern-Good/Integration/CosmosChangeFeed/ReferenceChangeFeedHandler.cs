using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Reviews.Infrastructure.Cosmos.Documents;

namespace WebShop.Integration.CosmosChangeFeed;

// Watches the "reference" container's change feed and fans denormalized-field changes out into
// the "products" container - Brand/Shop/ReviewUser/ProductGroup renames are otherwise invisible
// to whatever Product/Price/Review documents already copied that field at write time (see each
// *Document.cs's own comment on why its snapshot exists). Classic (not AllVersionsAndDeletes)
// change feed mode is fine here: nothing in this domain ever deletes reference data, so the
// well-known "change feed can't see deletes" gap - the reason Review uses SoftDelete instead of a
// real delete - doesn't apply to Brand/Shop/ReviewUser/ProductGroup.
public sealed class ReferenceChangeFeedHandler(CosmosClient client, CosmosOptions options)
{
    private Container Products => client.GetContainer(options.DatabaseName, CosmosContainers.Products);

    public async Task HandleChangesAsync(IReadOnlyCollection<JObject> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            switch ((string?)change["type"])
            {
                case "Brand":
                    await RefreshBrandSnapshotsAsync(change, cancellationToken);
                    break;
                case "Shop":
                    await RefreshShopSnapshotsAsync(change, cancellationToken);
                    break;
                case "ReviewUser":
                    await RefreshReviewerSnapshotsAsync(change, cancellationToken);
                    break;
                case "ProductGroup":
                    await RefreshGroupPathLeafAsync(change, cancellationToken);
                    break;
            }
        }
    }

    private async Task RefreshBrandSnapshotsAsync(JObject brand, CancellationToken cancellationToken)
    {
        var brandId = (int)brand["brandId"]!;
        var name = (string)brand["name"]!;

        var query = new QueryDefinition("SELECT c.id, c.productId FROM c WHERE c.type = 'Product' AND c.brandId = @brandId")
            .WithParameter("@brandId", brandId);

        await foreach (var (id, productId) in QueryMatchesAsync(query, cancellationToken))
            await Products.PatchItemAsync<object>(id, new PartitionKey(productId),
                [PatchOperation.Set("/brandName", name)], cancellationToken: cancellationToken);
    }

    private async Task RefreshShopSnapshotsAsync(JObject shop, CancellationToken cancellationToken)
    {
        var shopId = (int)shop["shopId"]!;
        var name = (string)shop["name"]!;
        var logo = (string?)shop["logo"];
        var rating = (double)shop["rating"]!;

        var query = new QueryDefinition("SELECT c.id, c.productId FROM c WHERE c.type = 'Price' AND c.shopId = @shopId")
            .WithParameter("@shopId", shopId);

        await foreach (var (id, productId) in QueryMatchesAsync(query, cancellationToken))
        {
            // logo is NullValueHandling.Ignore on PriceDocument (absent, not null, when there is
            // none) - Remove rather than Set(null) keeps that same "absent means none" shape.
            var patch = new List<PatchOperation> { PatchOperation.Set("/shopName", name), PatchOperation.Set("/shopRating", rating) };
            patch.Add(logo is null ? PatchOperation.Remove("/shopLogo") : PatchOperation.Set("/shopLogo", logo));
            await Products.PatchItemAsync<object>(id, new PartitionKey(productId), patch, cancellationToken: cancellationToken);
        }
    }

    private async Task RefreshReviewerSnapshotsAsync(JObject reviewUser, CancellationToken cancellationToken)
    {
        var reviewUserId = (int)reviewUser["reviewUserId"]!;
        var snapshot = new ReviewerSnapshot { Name = (string)reviewUser["name"]!, Email = (string?)reviewUser["email"] };

        var query = new QueryDefinition("SELECT c.id, c.productId FROM c WHERE c.type = 'Review' AND c.reviewUserId = @reviewUserId")
            .WithParameter("@reviewUserId", reviewUserId);

        await foreach (var (id, productId) in QueryMatchesAsync(query, cancellationToken))
            await Products.PatchItemAsync<object>(id, new PartitionKey(productId),
                [PatchOperation.Set("/reviewer", snapshot)], cancellationToken: cancellationToken);
    }

    // Only refreshes a product's OWN leaf group entry - a product whose ancestor (not immediate)
    // group was renamed keeps a stale breadcrumb entry for that ancestor further up its
    // groupPath. Cascading a rename through every descendant group's products would mean walking
    // the whole ProductGroup tree from the changed node down on every single change; deliberately
    // out of scope here - group renames are rare, and the breadcrumb is a display nicety, not
    // data anything else in the design depends on.
    private async Task RefreshGroupPathLeafAsync(JObject group, CancellationToken cancellationToken)
    {
        var groupId = (int)group["productGroupId"]!;
        var name = (string)group["name"]!;

        var query = new QueryDefinition("SELECT c.id, c.productId, c.groupPath FROM c WHERE c.type = 'Product' AND c.productGroupId = @groupId")
            .WithParameter("@groupId", groupId);

        using var iterator = Products.GetItemQueryIterator<JObject>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
            {
                var groupPath = (JArray)row["groupPath"]!;
                if (groupPath.Count == 0)
                    continue;

                await Products.PatchItemAsync<object>((string)row["id"]!, new PartitionKey((int)row["productId"]!),
                    [PatchOperation.Set($"/groupPath/{groupPath.Count - 1}/name", name)], cancellationToken: cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<(string Id, int ProductId)> QueryMatchesAsync(
        QueryDefinition query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var iterator = Products.GetItemQueryIterator<JObject>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in page)
                yield return ((string)row["id"]!, (int)row["productId"]!);
        }
    }
}
