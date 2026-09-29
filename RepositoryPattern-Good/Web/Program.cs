using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebShop.BuildingBlocks.Api;
using WebShop.Web.Identity;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Services.Pricing;
using WebShop.Web.Services.Reviews;
using WebShop.Web.Services.Search;

var builder = WebApplication.CreateBuilder(args);

builder.AddWebShopObservability("webshop-web");

builder.Services.AddRazorPages();

// Without this, each replica generates its own Data Protection key ring in memory - fine for a
// single instance, but fatal the moment there's more than one (see k8s/15-web.yaml's Deployment,
// replicas: 2): a page rendered by one Pod embeds an antiforgery token (or issues an auth cookie)
// encrypted with ITS key, and if the next request lands on a different Pod via the Service's
// load balancing, that Pod can't decrypt it - antiforgery validation throws
// CryptographicException("key was not found in the key ring") and ASP.NET Core turns that into a
// bare 400 with no further detail, which is exactly what surfaces as "submitting this form
// intermittently fails" with nothing in the browser to explain why. Pointing every replica at the
// same shared directory (see k8s/15-web.yaml's dataprotection-keys volume - an Azure Files share,
// not a Managed Disk, specifically because it supports being mounted read-write by multiple Pods
// at once) means they all read/write the same keys and can decrypt each other's tokens/cookies.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("WebShop.Web")
        .PersistKeysToFileSystem(new DirectoryInfo("/keys"));
}

// Skipped under the "Testing" environment: Web.Tests registers this itself against a SQLite
// :memory: connection (see WebApiFactory) instead. Registering Npgsql here unconditionally would
// leave its provider services in the container alongside Sqlite's, which EF Core rejects outright
// ("only a single database provider can be registered") - conditional registration, not just
// swapping DbContextOptions, is the only way around that.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// Mirrors Reviews.Api's "CanAdministerReviews" policy (same claim, same value) - Web needs its
// own copy since it authorizes the /Admin pages against the cookie principal, not a JWT.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAdministerReviews", policy => policy.RequireClaim("permission", "reviews:administer"));
});

builder.Services.AddSingleton<JwtTokenService>();

// Old, direct-URL wiring - commented out, not deleted (same convention as the Postgres->Cosmos
// swap - see feedback_comment_dont_delete_provider_code in project memory). This is exactly what
// DaprServiceInvocation.ResolveServiceUri below falls back to when no Dapr sidecar is present
// (docker-compose, `dotnet run` locally), so nothing about local dev actually changed - only AKS
// (where every Deployment now carries a dapr.io/app-id annotation, see k8s/10-catalog.yaml) picks
// up the new Dapr-routed path, automatically, via DAPR_HTTP_PORT's presence.
// builder.Services.AddHttpClient<CatalogApiClient>(client =>
//     client.BaseAddress = new Uri(builder.Configuration["Services:Catalog"]!));
// builder.Services.AddHttpClient<PricingApiClient>(client =>
//     client.BaseAddress = new Uri(builder.Configuration["Services:Pricing"]!));
// builder.Services.AddHttpClient<ReviewsApiClient>(client =>
//     client.BaseAddress = new Uri(builder.Configuration["Services:Reviews"]!));
// builder.Services.AddHttpClient<SearchApiClient>(client =>
//     client.BaseAddress = new Uri(builder.Configuration["Services:Search"]!));

builder.Services.AddHttpClient<CatalogApiClient>()
    .ConfigureServiceInvocation(builder.Configuration, "catalog", "Services:Catalog");
builder.Services.AddHttpClient<PricingApiClient>()
    .ConfigureServiceInvocation(builder.Configuration, "pricing", "Services:Pricing");
builder.Services.AddHttpClient<ReviewsApiClient>()
    .ConfigureServiceInvocation(builder.Configuration, "reviews", "Services:Reviews");
builder.Services.AddHttpClient<SearchApiClient>()
    .ConfigureServiceInvocation(builder.Configuration, "search", "Services:Search");

// Alternative not taken: Dapr's own SDK (the `Dapr.Client` package) instead of routing a plain
// HttpClient through the sidecar's HTTP invoke-URL prefix. It would look roughly like this:
//
//   builder.Services.AddDaprClient();   // registers a singleton DaprClient
//
//   // ...and every *ApiClient (CatalogApiClient, PricingApiClient, etc.) would take a DaprClient
//   // instead of an HttpClient, e.g. CatalogApiClient.GetProductByIdAsync becomes:
//   public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken ct)
//   {
//       try
//       {
//           return await daprClient.InvokeMethodAsync<ProductDto>(HttpMethod.Get, "catalog", $"products/{id}", ct);
//       }
//       catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
//       {
//           return null;
//       }
//   }
//
// InvokeMethodAsync hits the same sidecar endpoint DaprServiceInvocation.ConfigureServiceInvocation
// routes through, so there's no behavioral gap for plain service invocation - the difference is blast
// radius. This approach only touches the registration line above; the DaprClient approach touches
// every call site in every *ApiClient (plus the direct-URL fallback would need its own HttpClient
// path preserved alongside DaprClient, doubling each client's constructor). Worth revisiting if
// this app ever adopts Dapr's state store/pub-sub/secrets building blocks through the same client,
// since those have no plain-HttpClient equivalent to fall back to.

// See Catalog/Api/Program.cs for why this doesn't also check Postgres (or the four downstream
// APIs') reachability.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Skipped under the "Testing" environment: Web.Tests swaps in a SQLite :memory: connection
// (see WebApiFactory) and creates its schema directly, since these Npgsql-authored migrations
// don't translate to SQLite.
if (!app.Environment.IsEnvironment("Testing"))
    await app.Services.EnsureIdentitySchemaCreatedAsync();

await app.Services.EnsureAdminClaimsAsync();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapRazorPages();

app.Run();

// Makes the compiler-generated top-level Program class visible to WebApplicationFactory<Program>
// in Web.Tests, which otherwise has no way to reference it (top-level statements produce an
// internal class by default).
public partial class Program;
