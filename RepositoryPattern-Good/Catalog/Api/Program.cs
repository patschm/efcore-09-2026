using WebShop.BuildingBlocks.Api;
using WebShop.Catalog.Api.Endpoints;
using WebShop.Catalog.Application;
#if COSMOS
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Catalog.Infrastructure.Cosmos;
#elif POSTGRES
using WebShop.Catalog.Infrastructure.Postgres;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Outbox;
#elif SQLSERVER
using WebShop.Catalog.Infrastructure.SqlServer;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddWebShopObservability("webshop-catalog");

#if COSMOS
// Bound loosely here (no eager validation) - Api.Tests's WebApplicationFactory only merges its
// config overrides in when Build() runs below, so anything read from builder.Configuration
// beforehand would still see the (deliberately empty) appsettings.json placeholder even under
// test. Real validation happens after Build(), gated the same way as the call below.
var cosmosOptions = builder.Configuration.GetSection("CosmosDb").Get<CosmosOptions>()
    ?? new CosmosOptions { ConnectionString = "", DatabaseName = "webshop" };
builder.Services.AddCatalogCosmosInfrastructure(cosmosOptions);
#elif POSTGRES
builder.Services.AddCatalogPostgresInfrastructure(builder.Configuration.GetConnectionString("Catalog")!);
#elif SQLSERVER
builder.Services.AddCatalogSqlServerInfrastructure(builder.Configuration.GetConnectionString("Catalog")!);
#endif
builder.Services.AddCatalogApplication();

// A bare liveness/readiness signal ("the process is up and the pipeline can respond") - not
// checked against the database itself, so it stays green even if the database is briefly
// unreachable rather than making Kubernetes kill and restart a pod that would recover on its
// own once the database comes back.
builder.Services.AddHealthChecks();

var app = builder.Build();

#if COSMOS
// Skipped under the "Testing" environment: integration tests replace every repository
// registration with in-memory fakes (see Api.Tests), so there's no real Cosmos account to
// validate a connection string for or bootstrap containers against.
if (!app.Environment.IsEnvironment("Testing"))
{
    if (string.IsNullOrEmpty(cosmosOptions.ConnectionString))
        throw new InvalidOperationException(
            "Set CosmosDb:ConnectionString (e.g. via the CosmosDb__ConnectionString environment variable) to the target Cosmos account's connection string.");

    await app.Services.EnsureCosmosContainersCreatedAsync();
}
#elif POSTGRES
// Skipped under the "Testing" environment: integration tests replace every repository/outbox
// registration with in-memory fakes (see Api.Tests), so there's no real CatalogPgContext
// connection to bootstrap and nothing here for them to wait on.
if (!app.Environment.IsEnvironment("Testing"))
    await app.Services.EnsureOutboxSchemaCreatedAsync();
#elif SQLSERVER
// Skipped under "Testing" for the same reason as the other providers above. Unlike Postgres,
// this provider has no pre-existing database to map onto - see SchemaInitializer for why this is
// EnsureCreated, not a migration or an outbox-only bootstrap.
if (!app.Environment.IsEnvironment("Testing"))
    await app.Services.EnsureSchemaCreatedAsync();
#endif

app.UseArgumentExceptionAsBadRequest();

app.MapHealthChecks("/health");

app.MapBrandEndpoints();
app.MapGroup("/product-groups").MapProductGroupEndpoints();
app.MapGroup("/products").MapProductEndpoints();

app.Run();

// Makes the compiler-generated top-level Program class visible to WebApplicationFactory<Program>
// in Api.Tests, which otherwise has no way to reference it (top-level statements produce an
// internal class by default).
public partial class Program;
