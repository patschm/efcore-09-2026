using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Repositories;
using PricingUnitOfWork = WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts.PricingUnitOfWork;

namespace WebShop.Pricing.Api.Tests;

// See CatalogApiFactory for why: SqlServer-flavored repositories against a real SQLite
// :memory: connection, swapped in for the real app's Cosmos wiring.
public sealed class PricingApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PricingApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<PricingContext>().UseSqlite(_connection).Options;
        using var context = new PricingContext(options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IShopRepository>();
            services.RemoveAll<IPriceRepository>();
            services.RemoveAll<IUnitOfWork>();

            services.AddDbContext<PricingContext>(options => options.UseSqlite(_connection));

            services.AddScoped<IShopRepository, ShopRepository>();
            services.AddScoped<IPriceRepository, PriceRepository>();
            services.AddScoped<IUnitOfWork, PricingUnitOfWork>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
