using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Pricing.Infrastructure.SqlServer.Persistence.Repositories;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.Tests;

public class PricePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<PricingContext> _options;

    public PricePersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<PricingContext>().UseSqlite(_connection).Options;
        using var setup = new PricingContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Price_round_trips_shop_and_shipping_money_through_the_complex_properties()
    {
        using (var context = new PricingContext(_options))
        {
            new ShopRepository(context).Add(Shop.Create(new ShopId(1), "Coolblue", new Url("https://www.coolblue.nl")));
            var price = Price.Create(
                new PriceId(1), new ProductId(1), new ShopId(1),
                new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10);
            new PriceRepository(context).Add(price);
            await context.SaveChangesAsync();
        }

        using var readContext = new PricingContext(_options);
        var loaded = await new PriceRepository(readContext).GetById(new PriceId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal(new Money(199.99, "EUR"), loaded.ShopPrice);
        Assert.Equal(new Money(204.99, "EUR"), loaded.ShippingPrice);
        Assert.Equal(10, loaded.InStock);
    }
}
