using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebShop.BuildingBlocks.Application.Abstractions.Outbox;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Outbox;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;
using CatalogUnitOfWork = WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts.CatalogUnitOfWork;

namespace WebShop.Catalog.Api.Tests;

// The real app wires Cosmos repositories against a real Cosmos account (see Program.cs). Tests
// instead swap in the SqlServer-flavored repositories against a real SQLite :memory: connection -
// proven equivalent for ordinary EF mappings in every other context's Infrastructure.Tests
// project, and it means these tests exercise real EF configurations, not hand-written fakes. A
// fresh connection/schema per factory instance, same as every PersistenceTests class elsewhere in
// this solution.
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CatalogApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<CatalogContext>().UseSqlite(_connection).Options;
        using var context = new CatalogContext(options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBrandRepository>();
            services.RemoveAll<IProductGroupRepository>();
            services.RemoveAll<IProductRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<IOutbox>();

            services.AddDbContext<CatalogContext>(options => options.UseSqlite(_connection));

            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IUnitOfWork, CatalogUnitOfWork>();
            services.AddScoped<IOutbox, EfOutbox>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
