using WebShop.BuildingBlocks.Api;
using WebShop.Pricing.Api.Endpoints;
using WebShop.Pricing.Application;
#if COSMOS
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Pricing.Infrastructure.Cosmos;
#elif POSTGRES
using WebShop.Pricing.Infrastructure.Postgres;
#elif SQLSERVER
using WebShop.Pricing.Infrastructure.SqlServer;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddWebShopObservability("webshop-pricing");

#if COSMOS
// See Catalog/Api/Program.cs for why validation is deferred until after Build().
var cosmosOptions = builder.Configuration.GetSection("CosmosDb").Get<CosmosOptions>()
    ?? new CosmosOptions { ConnectionString = "", DatabaseName = "webshop" };
builder.Services.AddPricingCosmosInfrastructure(cosmosOptions);
#elif POSTGRES
builder.Services.AddPricingPostgresInfrastructure(builder.Configuration.GetConnectionString("Pricing")!);
#elif SQLSERVER
builder.Services.AddPricingSqlServerInfrastructure(builder.Configuration.GetConnectionString("Pricing")!);
#endif
builder.Services.AddPricingApplication();

// See Catalog/Api/Program.cs for why this doesn't also check the database's reachability.
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
// POSTGRES: no bootstrap needed - Pricing's tables already exist in the legacy database.

app.UseArgumentExceptionAsBadRequest();

app.MapHealthChecks("/health");

app.MapShopEndpoints();
app.MapGroup("/prices").MapPriceEndpoints();

app.Run();

// Makes the compiler-generated top-level Program class visible to WebApplicationFactory<Program>
// in Api.Tests, which otherwise has no way to reference it (top-level statements produce an
// internal class by default).
public partial class Program;
