using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Repositories;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.Tests;

public class ShopPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<PricingContext> _options;

    public ShopPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<PricingContext>().UseSqlite(_connection).Options;
        using var setup = new PricingContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Shop_round_trips_including_url_and_rating()
    {
        using (var context = new PricingContext(_options))
        {
            var shop = Shop.Create(new ShopId(1), "Coolblue", new Url("https://www.coolblue.nl"), rating: 4.5);
            new ShopRepository(context).Add(shop);
            await context.SaveChangesAsync();
        }

        using var readContext = new PricingContext(_options);
        var loaded = await new ShopRepository(readContext).GetById(new ShopId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal("Coolblue", loaded.Name);
        Assert.Equal("https://www.coolblue.nl", loaded.Url.Value);
        Assert.Equal(4.5, loaded.Rating);
    }
}
