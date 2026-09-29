namespace WebShop.BuildingBlocks.Cosmos;

// Bound the same way a provider connection string is today (Configuration.GetSection("CosmosDb")),
// just for the one shared account every context's Cosmos infrastructure talks to.
public sealed class CosmosOptions
{
    public required string ConnectionString { get; init; }
    public string DatabaseName { get; init; } = "webshop";
}
