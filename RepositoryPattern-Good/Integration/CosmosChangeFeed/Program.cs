using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Integration.CosmosChangeFeed;
using WebShop.Search.Application.Commands;
using WebShop.Search.Infrastructure.Cosmos.Persistence;
using WebShop.Search.Infrastructure.Embedding;

// Long-running console host for the Cosmos change-feed relay (see ReferenceChangeFeedHandler and
// ProductSpecChangeFeedHandler for what it actually does). Plain top-level script, same shape as
// Tools/CosmosMigration - no DI container, everything constructed by hand once at startup.

var cosmosConnectionString = Environment.GetEnvironmentVariable("WEBSHOP_COSMOS_CONNECTION")
    ?? throw new InvalidOperationException("Set WEBSHOP_COSMOS_CONNECTION to the target Cosmos account's connection string.");
var databaseName = Environment.GetEnvironmentVariable("WEBSHOP_COSMOS_DATABASE") ?? "webshop";
var embeddingServerUrl = Environment.GetEnvironmentVariable("WEBSHOP_EMBEDDING_URL")
    ?? throw new InvalidOperationException(
        "Set WEBSHOP_EMBEDDING_URL to the embedding model server's base address (see Search/EmbeddingServer/README.md).");

var cosmosOptions = new CosmosOptions { ConnectionString = cosmosConnectionString, DatabaseName = databaseName };
using var cosmosClient = new CosmosClient(cosmosOptions.ConnectionString, new CosmosClientOptions
{
    MaxRetryAttemptsOnRateLimitedRequests = 30,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(120)
});

// Reuses Search's real embedding pipeline verbatim - see ProductSpecChangeFeedHandler's own
// comment for why. No DI: Qwen3EmbeddingClient just takes a plain HttpClient in its constructor.
using var embeddingHttpClient = new HttpClient { BaseAddress = new Uri(embeddingServerUrl) };
var upsertEmbedding = new UpsertProductEmbeddingCommandHandler(
    new Qwen3EmbeddingClient(embeddingHttpClient),
    new ProductEmbeddingRepository(cosmosClient, cosmosOptions));

var referenceHandler = new ReferenceChangeFeedHandler(cosmosClient, cosmosOptions);
var productsHandler = new ProductSpecChangeFeedHandler(cosmosClient, cosmosOptions, upsertEmbedding);

const string leaseContainerName = "leases";
await CosmosContainerProvisioner.EnsureChangeFeedLeaseContainerCreatedAsync(cosmosClient, databaseName, leaseContainerName);
var leaseContainer = cosmosClient.GetContainer(databaseName, leaseContainerName);

// Both processors share one lease container, distinguished by processorName (the SDK prefixes
// each processor's lease documents with it) - the supported way to run more than one processor
// against a single lease container.
var referenceProcessor = cosmosClient.GetContainer(databaseName, CosmosContainers.Reference)
    .GetChangeFeedProcessorBuilder<JObject>("reference-relay", (changes, ct) => referenceHandler.HandleChangesAsync(changes, ct))
    .WithInstanceName(Environment.MachineName)
    .WithLeaseContainer(leaseContainer)
    .WithStartTime(DateTime.MinValue.ToUniversalTime())
    .Build();

var productsProcessor = cosmosClient.GetContainer(databaseName, CosmosContainers.Products)
    .GetChangeFeedProcessorBuilder<JObject>("products-spec-relay", (changes, ct) => productsHandler.HandleChangesAsync(changes, ct))
    .WithInstanceName(Environment.MachineName)
    .WithLeaseContainer(leaseContainer)
    .WithStartTime(DateTime.MinValue.ToUniversalTime())
    .Build();

await referenceProcessor.StartAsync();
await productsProcessor.StartAsync();
Console.WriteLine("Change feed processors started - watching 'reference' and 'products'. Ctrl+C to stop.");

// No Microsoft.Extensions.Hosting here to hook shutdown for us, so Ctrl+C/SIGTERM is wired by
// hand - without this, killing the process would drop leases mid-batch instead of stopping clean.
var shutdown = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // keep the process alive long enough for StopAsync to run below
    shutdown.TrySetResult();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdown.TrySetResult();

await shutdown.Task;

await referenceProcessor.StopAsync();
await productsProcessor.StopAsync();
Console.WriteLine("Change feed processors stopped.");
