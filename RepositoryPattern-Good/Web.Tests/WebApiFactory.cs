using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Web.Identity;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Services.Pricing;
using WebShop.Web.Services.Reviews;
using WebShop.Web.Services.Search;
using WebShop.Web.Tests.Fakes;

namespace WebShop.Web.Tests;

// See ReviewsApiFactory (Reviews/Api.Tests) for the same idea: a real SQLite :memory: connection
// swapped in for Web's own Postgres-backed Identity store, so UserManager/SignInManager exercise
// the genuine EF Core identity stack instead of a fake. The four downstream bounded contexts are
// never actually reachable from a test run, so their typed HttpClients each get a fake handler
// instead - CatalogHandler/PricingHandler/ReviewsHandler/SearchHandler - programmed per test with
// exactly the responses that scenario needs.
public sealed class WebApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeHttpMessageHandler CatalogHandler { get; } = new();
    public FakeHttpMessageHandler PricingHandler { get; } = new();
    public FakeHttpMessageHandler ReviewsHandler { get; } = new();
    public FakeHttpMessageHandler SearchHandler { get; } = new();

    public WebApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationIdentityDbContext>().UseSqlite(_connection).Options;
        using var context = new ApplicationIdentityDbContext(options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Explicit rather than relying on appsettings.json - must match the key JwtTokenService
        // signs access tokens with when RegisterModel/DetailsModel call the (faked) Reviews API.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "Kx7mQ2vL9pR4tN8wZ1yB6cF3hJ5sV0uE9oI2aD7gM4kP1rT6xC8lW3nA5qY0bHs",
            ["Jwt:Issuer"] = "WebShop.Web",
            ["Jwt:Audience"] = "WebShop.Reviews"
        }));

        builder.ConfigureServices(services =>
        {
            // Program.cs skips its own (Npgsql) registration under "Testing" specifically so this
            // is the only registration in play - see the comment there for why.
            services.AddDbContext<ApplicationIdentityDbContext>(options => options.UseSqlite(_connection));

            services.AddHttpClient<CatalogApiClient>(client => client.BaseAddress = new Uri("http://catalog.test"))
                .ConfigurePrimaryHttpMessageHandler(() => CatalogHandler);
            services.AddHttpClient<PricingApiClient>(client => client.BaseAddress = new Uri("http://pricing.test"))
                .ConfigurePrimaryHttpMessageHandler(() => PricingHandler);
            services.AddHttpClient<ReviewsApiClient>(client => client.BaseAddress = new Uri("http://reviews.test"))
                .ConfigurePrimaryHttpMessageHandler(() => ReviewsHandler);
            services.AddHttpClient<SearchApiClient>(client => client.BaseAddress = new Uri("http://search.test"))
                .ConfigurePrimaryHttpMessageHandler(() => SearchHandler);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
