using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Infrastructure.Tests;

// Uses the SqlServer-flavored CatalogContext against real SQLite in-memory - ordinary EF
// mappings (conversions, complex properties, collections) don't differ between providers for
// this purpose, same trick already proven in the project's manual smoke tests.
public class BrandPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<CatalogContext> _options;

    public BrandPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<CatalogContext>().UseSqlite(_connection).Options;
        using var setup = new CatalogContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Brand_round_trips_including_website_and_logo_urls()
    {
        using (var context = new CatalogContext(_options))
        {
            var brand = Brand.Create(
                new BrandId(1), "Samsung", new Url("https://www.samsung.com"), new Url("https://www.samsung.com/logo.png"));
            new BrandRepository(context).Add(brand);
            await context.SaveChangesAsync();
        }

        using var readContext = new CatalogContext(_options);
        var loaded = await new BrandRepository(readContext).GetById(new BrandId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal("Samsung", loaded.Name);
        Assert.Equal("https://www.samsung.com", loaded.Website?.Value);
        Assert.Equal("https://www.samsung.com/logo.png", loaded.Logo?.Value);
    }

    [Fact]
    public async Task Brand_without_website_or_logo_round_trips_as_null()
    {
        using (var context = new CatalogContext(_options))
        {
            new BrandRepository(context).Add(Brand.Create(new BrandId(1), "Samsung"));
            await context.SaveChangesAsync();
        }

        using var readContext = new CatalogContext(_options);
        var loaded = await new BrandRepository(readContext).GetById(new BrandId(1), default);

        Assert.NotNull(loaded);
        Assert.Null(loaded.Website);
        Assert.Null(loaded.Logo);
    }
}
