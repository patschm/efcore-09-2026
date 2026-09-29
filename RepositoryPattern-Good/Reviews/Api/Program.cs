using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebShop.BuildingBlocks.Api;
using WebShop.Reviews.Api.Endpoints;
using WebShop.Reviews.Application;
#if COSMOS
using WebShop.BuildingBlocks.Cosmos;
using WebShop.Reviews.Infrastructure.Cosmos;
#elif POSTGRES
using WebShop.Reviews.Infrastructure.Postgres;
#elif SQLSERVER
using WebShop.Reviews.Infrastructure.SqlServer;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddWebShopObservability("webshop-reviews");

#if COSMOS
// See Catalog/Api/Program.cs for why validation is deferred until after Build().
var cosmosOptions = builder.Configuration.GetSection("CosmosDb").Get<CosmosOptions>()
    ?? new CosmosOptions { ConnectionString = "", DatabaseName = "webshop" };
builder.Services.AddReviewsCosmosInfrastructure(cosmosOptions);
#elif POSTGRES
builder.Services.AddReviewsPostgresInfrastructure(builder.Configuration.GetConnectionString("Reviews")!);
#elif SQLSERVER
builder.Services.AddReviewsSqlServerInfrastructure(builder.Configuration.GetConnectionString("Reviews")!);
#endif
builder.Services.AddReviewsApplication();

// Validates tokens the Web BFF issues after a user logs in - see WebShop.Web's JwtTokenService
// for the matching issuer. No separate auth server: Web and Reviews just share this signing key.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateLifetime = true
        };
    });
// "permission" claim values, minted by Web's JwtTokenService from the caller's Identity claims -
// two separate permissions rather than one "authenticated" check, so a plain reviewer's token
// can never be used to administer (e.g. delete) someone else's review.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateReviews", policy => policy.RequireClaim("permission", "reviews:create"));
    options.AddPolicy("CanAdministerReviews", policy => policy.RequireClaim("permission", "reviews:administer"));
});

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
// POSTGRES: no bootstrap needed - Reviews's tables already exist in the legacy database.

app.UseArgumentExceptionAsBadRequest();

app.UseAuthentication();
app.UseAuthorization();

// Ahead of auth middleware's effect on routing but MapHealthChecks has no [Authorize] of its
// own, so it's reachable regardless - a kubelet probe carries no JWT/cookie to present.
app.MapHealthChecks("/health");

app.MapReviewUserEndpoints();
app.MapGroup("/reviews").MapReviewEndpoints();

app.Run();

// Makes the compiler-generated top-level Program class visible to WebApplicationFactory<Program>
// in Api.Tests, which otherwise has no way to reference it (top-level statements produce an
// internal class by default).
public partial class Program;
