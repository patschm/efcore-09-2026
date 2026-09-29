using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Repositories;
using ReviewsUnitOfWork = WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts.ReviewsUnitOfWork;

namespace WebShop.Reviews.Api.Tests;

// See CatalogApiFactory for why: SqlServer-flavored repositories against a real SQLite
// :memory: connection, swapped in for the real app's Cosmos wiring.
public sealed class ReviewsApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ReviewsApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ReviewsContext>().UseSqlite(_connection).Options;
        using var context = new ReviewsContext(options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Explicit rather than relying on appsettings.json being copied into the test output -
        // must match the key TestJwt signs with.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "Kx7mQ2vL9pR4tN8wZ1yB6cF3hJ5sV0uE9oI2aD7gM4kP1rT6xC8lW3nA5qY0bHs",
            ["Jwt:Issuer"] = "WebShop.Web",
            ["Jwt:Audience"] = "WebShop.Reviews"
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IReviewUserRepository>();
            services.RemoveAll<IReviewRepository>();
            services.RemoveAll<IUnitOfWork>();

            services.AddDbContext<ReviewsContext>(options => options.UseSqlite(_connection));

            services.AddScoped<IReviewUserRepository, ReviewUserRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IUnitOfWork, ReviewsUnitOfWork>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
