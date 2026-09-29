using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Npgsql;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.Catalog.Infrastructure.Cosmos.Adapters;
using WebShop.Catalog.Infrastructure.Cosmos.Documents;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.Pricing.Infrastructure.Cosmos.Adapters;
using WebShop.Reviews.Infrastructure.Cosmos.Adapters;
using WebShop.Search.Infrastructure.Cosmos.Adapters;
using WebShop.SharedKernel;
using PricingProductId = WebShop.Pricing.Domain.Identifiers.ProductId;
using PricingShop = WebShop.Pricing.Domain.Aggregates.Shop;
using PricingShopId = WebShop.Pricing.Domain.Identifiers.ShopId;
using PricingPrice = WebShop.Pricing.Domain.Aggregates.Price;
using PricingPriceId = WebShop.Pricing.Domain.Identifiers.PriceId;
using ReviewsProductId = WebShop.Reviews.Domain.Identifiers.ProductId;
using ReviewsReview = WebShop.Reviews.Domain.Aggregates.Review;
using ReviewsReviewId = WebShop.Reviews.Domain.Identifiers.ReviewId;
using ReviewsReviewUser = WebShop.Reviews.Domain.Aggregates.ReviewUser;
using ReviewsReviewUserId = WebShop.Reviews.Domain.Identifiers.ReviewUserId;
using SearchProductId = WebShop.Search.Domain.Identifiers.ProductId;
using SearchProductEmbedding = WebShop.Search.Domain.Aggregates.ProductEmbedding;

// One-off ETL: reads the legacy flat Postgres schema (the "webshop" db's public schema) and
// writes into the two shared Cosmos containers, reusing each context's real adapters/domain
// factories so the migrated data is byte-for-byte what the online repositories would have
// produced. Not part of any deployed service - run manually, once, from the command line.

var pgConnectionString = Environment.GetEnvironmentVariable("WEBSHOP_PG_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=webshop;Username=postgres;Password=postgres";
var cosmosConnectionString = Environment.GetEnvironmentVariable("WEBSHOP_COSMOS_CONNECTION")
    ?? throw new InvalidOperationException("Set WEBSHOP_COSMOS_CONNECTION to the target Cosmos account's connection string.");

await using var dataSource = NpgsqlDataSource.Create(pgConnectionString);

using var cosmosClient = new CosmosClient(cosmosConnectionString, new CosmosClientOptions
{
    AllowBulkExecution = true,
    // The container starts at the autoscale floor (100 RU/s, 10% of the 1000 max) and only
    // ramps up under sustained load - a generous retry budget lets the SDK ride out 429s while
    // that ramp-up happens instead of giving up and crashing the whole migration.
    MaxRetryAttemptsOnRateLimitedRequests = 30,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(120)
});

Console.WriteLine("Ensuring containers exist (autoscale, max 1000 RU/s each - the cheapest provisioned tier)...");
await CosmosContainerProvisioner.EnsureContainersCreatedAsync(
    cosmosClient, "webshop", containerThroughput: ThroughputProperties.CreateAutoscaleThroughput(1000));

var referenceContainer = cosmosClient.GetContainer("webshop", CosmosContainers.Reference);
var productsContainer = cosmosClient.GetContainer("webshop", CosmosContainers.Products);

// Declared before the early-exit checks below (--verify/--clean-specvalues/--export/--import all
// return before the migration phases further down would otherwise assign this) since several of
// those modes' helper functions reference it for progress logging.
var stopwatch = Stopwatch.StartNew();

if (args.Contains("--verify"))
{
    await VerifyCountsAsync();
    return;
}

if (args.Contains("--clean-specvalues"))
{
    await CleanSpecValuesAsync();
    return;
}

if (GetArgValue(args, "--export") is { } exportDir)
{
    await ExportAsync(exportDir);
    return;
}

if (GetArgValue(args, "--import") is { } importDir)
{
    await ImportAsync(importDir);
    return;
}

string? GetArgValue(string[] a, string flag)
{
    var idx = Array.IndexOf(a, flag);
    return idx >= 0 && idx + 1 < a.Length ? a[idx + 1] : null;
}

// Dumps every item in both containers to newline-delimited JSON, one file per container -
// portable to any other Cosmos account (or the local emulator) via --import, and independent
// of whichever specific document DTO classes exist in code (round-trips raw JSON, untyped).
async Task ExportAsync(string directory)
{
    Directory.CreateDirectory(directory);
    await ExportContainerAsync(referenceContainer, Path.Combine(directory, "reference.ndjson"));
    await ExportContainerAsync(productsContainer, Path.Combine(directory, "products.ndjson"));
    Console.WriteLine($"Export complete: {Path.GetFullPath(directory)}");
}

async Task ExportContainerAsync(Container container, string filePath)
{
    await using var writer = new StreamWriter(filePath, append: false);
    var count = 0;
    using var iterator = container.GetItemQueryIterator<Newtonsoft.Json.Linq.JObject>(new QueryDefinition("SELECT * FROM c"));
    while (iterator.HasMoreResults)
    {
        foreach (var item in await iterator.ReadNextAsync())
        {
            // Strip Cosmos-managed system properties - they're per-account artifacts (an _etag
            // from this account means nothing on another), not part of the actual data.
            item.Remove("_rid"); item.Remove("_self"); item.Remove("_etag"); item.Remove("_attachments"); item.Remove("_ts");
            await writer.WriteLineAsync(item.ToString(Newtonsoft.Json.Formatting.None));
            count++;
        }

        Console.WriteLine($"  exported {count} from {container.Id}");
    }
}

async Task ImportAsync(string directory)
{
    await ImportContainerAsync(referenceContainer, Path.Combine(directory, "reference.ndjson"));
    await ImportContainerAsync(productsContainer, Path.Combine(directory, "products.ndjson"));
    Console.WriteLine("Import complete.");
}

async Task ImportContainerAsync(Container container, string filePath)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"  [skip] {filePath} not found");
        return;
    }

    // Partition key path (e.g. "/type" or "/productId") is read from the container itself,
    // rather than hardcoded, so this works against whichever account WEBSHOP_COSMOS_CONNECTION
    // currently points to - point it at a fresh account first, then run --import.
    var pkPath = (await container.ReadContainerAsync()).Resource.PartitionKeyPath.TrimStart('/');

    var batch = new List<(PartitionKey PartitionKey, object Item)>();
    var total = 0;
    foreach (var line in File.ReadLines(filePath))
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var item = Newtonsoft.Json.Linq.JObject.Parse(line);
        var pkToken = item[pkPath] ?? throw new InvalidOperationException($"Missing partition key '{pkPath}' in a line of {filePath}");
        var partitionKey = pkToken.Type is Newtonsoft.Json.Linq.JTokenType.Integer or Newtonsoft.Json.Linq.JTokenType.Float
            ? new PartitionKey((double)pkToken)
            : new PartitionKey((string?)pkToken ?? throw new InvalidOperationException($"Null partition key '{pkPath}' in a line of {filePath}"));

        batch.Add((partitionKey, item));
        total++;

        if (batch.Count >= 200)
        {
            await UpsertBatchWithRetryAsync(container, batch);
            Console.WriteLine($"  imported {total} into {container.Id} ({stopwatch.Elapsed})");
            batch.Clear();
        }
    }

    if (batch.Count > 0)
        await UpsertBatchWithRetryAsync(container, batch);

    Console.WriteLine($"  imported {total} total into {container.Id}");
}

async Task VerifyCountsAsync()
{
    async Task<int> CountAsync(Container container, string type)
    {
        using var iterator = container.GetItemQueryIterator<int>(
            new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = @type").WithParameter("@type", type));
        var page = await iterator.ReadNextAsync();
        return page.FirstOrDefault();
    }

    foreach (var type in new[] { "Brand", "ProductGroup", "Shop", "ReviewUser" })
        Console.WriteLine($"reference/{type}: {await CountAsync(referenceContainer, type)}");

    foreach (var type in new[] { "Product", "SpecValue", "Price", "Review", "Embedding" })
        Console.WriteLine($"products/{type}: {await CountAsync(productsContainer, type)}");
}

// One-time cleanup for the SpecValueDocument id-scheme fix: the old specDefId-keyed items
// collapsed multi-valued specs onto one document each, so they need clearing before the
// corrected (value-id-keyed) documents can be written without leaving orphaned old items behind.
async Task CleanSpecValuesAsync()
{
    var toDelete = new List<(PartitionKey PartitionKey, string Id)>();
    using var iterator = productsContainer.GetItemQueryIterator<SpecValueKey>(
        new QueryDefinition("SELECT c.id, c.productId FROM c WHERE c.type = 'SpecValue'"));
    while (iterator.HasMoreResults)
    {
        var page = await iterator.ReadNextAsync();
        toDelete.AddRange(page.Select(k => (new PartitionKey(k.ProductId), k.Id)));
    }

    Console.WriteLine($"Deleting {toDelete.Count} existing SpecValue items...");
    var localStopwatch = Stopwatch.StartNew();
    const int batchSize = 200;
    for (var offset = 0; offset < toDelete.Count; offset += batchSize)
    {
        var batch = toDelete.GetRange(offset, Math.Min(batchSize, toDelete.Count - offset));
        await Task.WhenAll(batch.Select(i => productsContainer.DeleteItemAsync<object>(i.Id, i.PartitionKey)));
        Console.WriteLine($"  deleted {Math.Min(offset + batchSize, toDelete.Count)}/{toDelete.Count} ({localStopwatch.Elapsed})");
    }

    Console.WriteLine("Cleanup done.");
}

// --- Reference data first: Products/Prices/Reviews below denormalize from these in-memory. ---

var brandNameById = new Dictionary<int, string>();
await MigrateBrandsAsync();

var (groupNameById, groupParentById) = (new Dictionary<int, string>(), new Dictionary<int, int?>());
var specDefinitionsByGroupId = new Dictionary<int, List<SpecificationDefinition>>();
await MigrateProductGroupsAsync();

var shopById = new Dictionary<int, PricingShop>();
await MigrateShopsAsync();

var reviewUserById = new Dictionary<int, ReviewsReviewUser>();
await MigrateReviewUsersAsync();

// --- Product-partitioned data: everything below lives in the "products" container. ---

// Set to resume after a prior run already completed the (slow) Product+SpecValue phase -
// upserts are idempotent, so redoing it would be harmless, just an avoidable ~hour of reruns.
if (Environment.GetEnvironmentVariable("WEBSHOP_MIGRATION_SKIP_PRODUCTS") == "true")
    Console.WriteLine("Skipping Product+SpecValue migration (WEBSHOP_MIGRATION_SKIP_PRODUCTS=true - already completed in a prior run).");
else
    await MigrateProductsAsync();

// Prices/Reviews/Embeddings are unaffected by the SpecValue id-scheme fix (their ids are
// already keyed by shopId/reviewId/productId, each genuinely unique per item) - skip re-running
// them when only fixing up Products+SpecValue.
if (Environment.GetEnvironmentVariable("WEBSHOP_MIGRATION_ONLY_PRODUCTS") == "true")
{
    Console.WriteLine("WEBSHOP_MIGRATION_ONLY_PRODUCTS=true - skipping Price/Review/Embedding (unaffected by this fix, already migrated).");
}
else
{
    await MigratePricesAsync();
    await MigrateReviewsAsync();
    await MigrateEmbeddingsAsync();
}

Console.WriteLine($"Done in {stopwatch.Elapsed}.");

async Task MigrateBrandsAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand("""SELECT "Id", "Name", "Website", "Logo" FROM "Brand" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var website = reader.IsDBNull(2) ? null : reader.GetString(2);
        var logo = reader.IsDBNull(3) ? null : reader.GetString(3);

        brandNameById[id] = name;

        try
        {
            var brand = Brand.Create(new BrandId(id), name, Url.TryCreate(website), Url.TryCreate(logo));
            var document = BrandDocumentAdapter.ToDocument(brand);
            items.Add((new PartitionKey("Brand"), document));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Brand {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(referenceContainer, items, "Brand");
}

async Task MigrateProductGroupsAsync()
{
    // SpecificationDefinition's own factory is internal to Catalog.Domain (only ProductGroup can
    // construct one, via DefineSpecification) - this tool is a different assembly, so raw rows
    // are held as plain tuples here and only turned into real SpecificationDefinition objects
    // once DefineSpecification (public) is called per group, below.
    var rawSpecDefinitionsByGroupId = new Dictionary<int, List<(int Id, string Key, string Name, string? Unit, string? Type, bool Multiple, string? Explanation)>>();
    await using (var cmd = dataSource.CreateCommand(
        """SELECT "Id", "ProductGroupId", "Key", "Name", "Unit", "Type", "Multiple", "Explanation" FROM "SpecificationDefinition" """))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            var groupId = reader.GetInt32(1);
            if (!rawSpecDefinitionsByGroupId.TryGetValue(groupId, out var list))
                rawSpecDefinitionsByGroupId[groupId] = list = [];

            list.Add((
                reader.GetInt32(0), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetBoolean(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        }
    }

    var items = new List<(PartitionKey, object)>();
    await using var groupCmd = dataSource.CreateCommand("""SELECT "Id", "Name", "ParentId", "ImageUrl" FROM "ProductGroup" """);
    await using var groupReader = await groupCmd.ExecuteReaderAsync();
    while (await groupReader.ReadAsync())
    {
        var id = groupReader.GetInt32(0);
        var name = groupReader.GetString(1);
        int? parentId = groupReader.IsDBNull(2) ? null : groupReader.GetInt32(2);
        var imageUrl = groupReader.IsDBNull(3) ? null : groupReader.GetString(3);

        groupNameById[id] = name;
        groupParentById[id] = parentId;

        try
        {
            var group = ProductGroup.Create(new ProductGroupId(id), name, parentId.HasValue ? new ProductGroupId(parentId.Value) : null, Url.TryCreate(imageUrl));

            foreach (var raw in rawSpecDefinitionsByGroupId.GetValueOrDefault(id, []))
            {
                var defineResult = group.DefineSpecification(
                    new SpecificationDefinitionId(raw.Id), raw.Key, raw.Name, raw.Type, raw.Unit, raw.Multiple, raw.Explanation);
                if (defineResult.IsFailure)
                {
                    Console.WriteLine($"  [skip spec] Group {id}, key '{raw.Key}': {string.Join("; ", defineResult.Errors)}");
                    continue;
                }

                if (!specDefinitionsByGroupId.TryGetValue(id, out var definitions))
                    specDefinitionsByGroupId[id] = definitions = [];
                definitions.Add(defineResult.Value);
            }

            items.Add((new PartitionKey("ProductGroup"), ProductGroupDocumentAdapter.ToDocument(group)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] ProductGroup {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(referenceContainer, items, "ProductGroup");
}

async Task MigrateShopsAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand("""SELECT "Id", "Name", "Url", "Logo", "Rating" FROM "Shop" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        try
        {
            var shop = PricingShop.Create(
                new PricingShopId(id),
                reader.GetString(1),
                new WebShop.SharedKernel.Url(reader.GetString(2)),
                Url.TryCreate(reader.IsDBNull(3) ? null : reader.GetString(3)),
                reader.GetDouble(4));

            shopById[id] = shop;
            items.Add((new PartitionKey("Shop"), ShopDocumentAdapter.ToDocument(shop)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Shop {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(referenceContainer, items, "Shop");
}

async Task MigrateReviewUsersAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand("""SELECT "Id", "Name", "Email" FROM "ReviewUser" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        try
        {
            var reviewUser = ReviewsReviewUser.Create(new ReviewsReviewUserId(id), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
            reviewUserById[id] = reviewUser;
            items.Add((new PartitionKey("ReviewUser"), ReviewUserDocumentAdapter.ToDocument(reviewUser)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] ReviewUser {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(referenceContainer, items, "ReviewUser");
}

async Task MigrateProductsAsync()
{
    var specValuesByProductId = new Dictionary<int, List<(ProductSpecificationValueId, SpecificationDefinitionId, SpecificationValue)>>();
    await using (var cmd = dataSource.CreateCommand(
        """SELECT "Id", "ProductId", "SpecificationDefinitionId", "NumberValue", "StringValue", "BoolValue" FROM "ProductSpecificationValue" """))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            var productId = reader.GetInt32(1);
            var valueResult = SpecificationValue.Create(
                reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetBoolean(5));

            if (valueResult.IsFailure)
            {
                Console.WriteLine($"  [skip] ProductSpecificationValue {reader.GetInt32(0)}: {string.Join("; ", valueResult.Errors)}");
                continue;
            }

            if (!specValuesByProductId.TryGetValue(productId, out var list))
                specValuesByProductId[productId] = list = [];
            list.Add((new ProductSpecificationValueId(reader.GetInt32(0)), new SpecificationDefinitionId(reader.GetInt32(2)), valueResult.Value));
        }
    }

    var items = new List<(PartitionKey, object)>();
    await using var productCmd = dataSource.CreateCommand("""SELECT "Id", "Name", "BrandId", "ProductGroupId", "ImageUrl" FROM "Product" """);
    await using var productReader = await productCmd.ExecuteReaderAsync();
    while (await productReader.ReadAsync())
    {
        var id = productReader.GetInt32(0);
        try
        {
            var brandId = productReader.GetInt32(2);
            int? productGroupId = productReader.IsDBNull(3) ? null : productReader.GetInt32(3);
            var imageUrl = productReader.IsDBNull(4) ? null : productReader.GetString(4);

            var definitionsById = productGroupId.HasValue
                ? specDefinitionsByGroupId.GetValueOrDefault(productGroupId.Value, []).ToDictionary(d => d.Id)
                : new Dictionary<SpecificationDefinitionId, SpecificationDefinition>();

            // Filter out mismatched values here (one bad row shouldn't drop the product's other,
            // valid spec values - see the [skip] logging below for what got excluded and why).
            var rawValues = specValuesByProductId.GetValueOrDefault(id, []);
            var validValues = new List<(ProductSpecificationValueId, SpecificationDefinitionId, SpecificationValue)>();
            foreach (var raw in rawValues)
            {
                if (definitionsById.ContainsKey(raw.Item2))
                    validValues.Add(raw);
                else
                    Console.WriteLine($"  [skip] Product {id}, SpecValue {raw.Item1.Value}: specification value for definition {raw.Item2.Value}, which isn't one of its product group's specification definitions.");
            }

            var product = Product.Reconstitute(
                new ProductId(id),
                productReader.GetString(1),
                new BrandId(brandId),
                productGroupId.HasValue ? new ProductGroupId(productGroupId.Value) : null,
                Url.TryCreate(imageUrl),
                validValues);

            var groupPath = BuildGroupPath(productGroupId);

            items.Add((new PartitionKey(id), ProductDocumentAdapter.ToDocument(product, brandNameById.GetValueOrDefault(brandId), groupPath)));
            foreach (var specDoc in ProductDocumentAdapter.ToSpecValueDocuments(product, definitionsById))
                items.Add((new PartitionKey(id), specDoc));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Product {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(productsContainer, items, "Product+SpecValue");
}

List<GroupPathEntry> BuildGroupPath(int? leafGroupId)
{
    var path = new List<GroupPathEntry>();
    var currentId = leafGroupId;
    while (currentId.HasValue && groupNameById.TryGetValue(currentId.Value, out var name))
    {
        path.Insert(0, new GroupPathEntry { Id = currentId.Value, Name = name });
        currentId = groupParentById.GetValueOrDefault(currentId.Value);
    }

    return path;
}

async Task MigratePricesAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand("""SELECT "Id", "ProductId", "ShopId", "ShopPrice", "ShippingPrice", "InStock" FROM "Price" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var productId = reader.GetInt32(1);
        var shopId = reader.GetInt32(2);
        try
        {
            // The legacy schema has no currency column - every shop/price row observed is a
            // Dutch price-comparison listing, so EUR is assumed rather than sourced.
            const string currency = "EUR";
            var price = PricingPrice.Create(
                new PricingPriceId(id),
                new PricingProductId(productId),
                new PricingShopId(shopId),
                new Money(reader.GetDouble(3), currency),
                new Money(reader.GetDouble(4), currency),
                reader.GetInt32(5));

            var shop = shopById.GetValueOrDefault(shopId);
            items.Add((new PartitionKey(productId), PriceDocumentAdapter.ToDocument(price, shop)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Price {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(productsContainer, items, "Price");
}

async Task MigrateReviewsAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand(
        """SELECT "Id", "ProductId", "Type", "Title", "Text", "Score", "ReviewUserId", "CreationDate" FROM "Review" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var productId = reader.GetInt32(1);
        try
        {
            int? reviewUserId = reader.IsDBNull(6) ? null : reader.GetInt32(6);
            var review = ReviewsReview.Create(
                new ReviewsReviewId(id),
                new ReviewsProductId(productId),
                reader.GetString(2),
                reader.GetFieldValue<DateOnly>(7),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                reviewUserId.HasValue ? new ReviewsReviewUserId(reviewUserId.Value) : null);

            var reviewUser = reviewUserId.HasValue ? reviewUserById.GetValueOrDefault(reviewUserId.Value) : null;
            items.Add((new PartitionKey(productId), ReviewDocumentAdapter.ToDocument(review, reviewUser)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Review {id}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(productsContainer, items, "Review");
}

async Task MigrateEmbeddingsAsync()
{
    var items = new List<(PartitionKey, object)>();
    await using var cmd = dataSource.CreateCommand(
        """SELECT "ProductId", CAST("Embedding" AS text) AS "Vector", "ContentHash", "GeneratedAt" FROM "ProductEmbeddingQwen3" """);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var productId = reader.GetInt32(0);
        try
        {
            var embedding = SearchProductEmbedding.Reconstitute(
                new SearchProductId(productId), reader.GetString(1), (byte[])reader.GetValue(2), reader.GetDateTime(3));
            items.Add((new PartitionKey(productId), EmbeddingDocumentAdapter.ToDocument(embedding)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [skip] Embedding {productId}: {ex.Message}");
        }
    }

    await BulkUpsertAsync(productsContainer, items, "Embedding");
}

async Task BulkUpsertAsync(Container container, List<(PartitionKey PartitionKey, object Item)> items, string label)
{
    // Starts small deliberately - the container begins at the autoscale floor (100 RU/s) and
    // only scales up in response to sustained demand, so a smaller burst here gives it room to
    // ramp up instead of immediately 429-ing before autoscale reacts.
    const int batchSize = 200;
    var written = 0;
    for (var offset = 0; offset < items.Count; offset += batchSize)
    {
        var batch = items.GetRange(offset, Math.Min(batchSize, items.Count - offset));
        await UpsertBatchWithRetryAsync(container, batch);
        written += batch.Count;
        Console.WriteLine($"  {label}: {written}/{items.Count} ({stopwatch.Elapsed})");
    }

    if (items.Count == 0)
        Console.WriteLine($"  {label}: nothing to write");
}

// Belt-and-suspenders on top of the client's own MaxRetryAttemptsOnRateLimitedRequests: a
// sustained throttling spike can still exhaust that budget (as happened after ~540k writes in
// an earlier run), which used to crash the whole multi-hour migration on one transient 429.
// This retries only the specific items that got throttled, with backoff, instead of giving up.
async Task UpsertBatchWithRetryAsync(Container container, List<(PartitionKey PartitionKey, object Item)> batch)
{
    var remaining = batch;
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        var failures = new List<(PartitionKey PartitionKey, object Item)>();
        var retryAfter = TimeSpan.Zero;
        var failuresLock = new object();

        await Task.WhenAll(remaining.Select(async item =>
        {
            try
            {
                await container.UpsertItemAsync(item.Item, item.PartitionKey);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                || ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout
                || ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                lock (failuresLock)
                {
                    failures.Add(item);
                    if (ex.RetryAfter.HasValue && ex.RetryAfter.Value > retryAfter)
                        retryAfter = ex.RetryAfter.Value;
                }
            }
        }));

        if (failures.Count == 0)
            return;

        remaining = failures;
        var wait = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(Math.Min(30, attempt * 2));
        Console.WriteLine($"    [throttled] retrying {remaining.Count} item(s) after {wait} (attempt {attempt})");
        await Task.Delay(wait);
    }

    throw new InvalidOperationException($"Gave up after repeated throttling on {remaining.Count} item(s).");
}

file sealed class SpecValueKey
{
    [Newtonsoft.Json.JsonProperty("id")] public string Id { get; set; } = null!;
    [Newtonsoft.Json.JsonProperty("productId")] public int ProductId { get; set; }
}
