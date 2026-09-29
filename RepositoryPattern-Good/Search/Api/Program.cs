using WebShop.BuildingBlocks.Api;
using WebShop.Search.Api.Endpoints;
using WebShop.Search.Application;
using WebShop.Search.Infrastructure.Embedding;
#if COSMOS
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Search.Infrastructure.Cosmos;
#elif POSTGRES
using WebShop.Search.Infrastructure.Postgres;
#elif SQLSERVER
using WebShop.Search.Infrastructure.SqlServer;
using WebShop.Search.Infrastructure.SqlServer.Persistence;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddWebShopObservability("webshop-search");

#if COSMOS
// See Catalog/Api/Program.cs for why validation is deferred until after Build().
var cosmosOptions = builder.Configuration.GetSection("CosmosDb").Get<CosmosOptions>()
    ?? new CosmosOptions { ConnectionString = "", DatabaseName = "webshop" };
builder.Services.AddSearchCosmosInfrastructure(cosmosOptions);
#elif POSTGRES
builder.Services.AddSearchPostgresInfrastructure(builder.Configuration.GetConnectionString("Search")!);
#elif SQLSERVER
builder.Services.AddSearchSqlServerInfrastructure(builder.Configuration.GetConnectionString("Search")!);
#endif
builder.Services.AddSearchApplication();

// Real model server, see Search/EmbeddingServer/README.md for what runs behind this URL.
// The Testing environment overrides IEmbeddingClient with a fake (SearchApiFactory) rather
// than hitting a real server, so this only ever runs against an actually-deployed one.
//
// Old, direct-URL wiring - commented out, not deleted (see Web/Program.cs for the fuller
// explanation of this pattern - DaprServiceInvocation.ResolveServiceUri below falls back to
// exactly this when no Dapr sidecar is present, e.g. docker-compose/`dotnet run`).
// builder.Services.AddQwen3EmbeddingClient(new Uri(builder.Configuration["Embedding:Url"]!));
builder.Services.AddQwen3EmbeddingClient(httpBuilder =>
    httpBuilder.ConfigureServiceInvocation(builder.Configuration, "embedding", "Embedding:Url"));

// See Catalog/Api/Program.cs for why this doesn't also check the database's (or the embedding
// server's) reachability.
builder.Services.AddHealthChecks();

var app = builder.Build();

#if COSMOS
if (!app.Environment.IsEnvironment("Testing"))
{
    if (string.IsNullOrEmpty(cosmosOptions.ConnectionString))
        throw new InvalidOperationException(
            "Set CosmosDb:ConnectionString (e.g. via the CosmosDb__ConnectionString environment variable) to the target Cosmos account's connection string.");

    await app.Services.EnsureCosmosContainersCreatedAsync();
}
#elif SQLSERVER
// See Catalog/Api/Program.cs's SQLSERVER branch for why this is EnsureCreated, not a migration.
if (!app.Environment.IsEnvironment("Testing"))
    await app.Services.EnsureSchemaCreatedAsync();
#endif
// POSTGRES: no bootstrap needed - Search's table already exists in the legacy database.

app.MapHealthChecks("/health");

app.MapEmbeddingEndpoints();
app.MapSearchEndpoints();
app.MapSimilarProductEndpoints();

app.Run();

// Makes the compiler-generated top-level Program class visible to WebApplicationFactory<Program>
// in Api.Tests, which otherwise has no way to reference it (top-level statements produce an
// internal class by default).
public partial class Program;
