using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Infrastructure.Cosmos.Adapters;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.Cosmos.Tests;

public class ShopDocumentAdapterTests
{
    [Fact]
    public void Round_trips_including_logo_and_rating()
    {
        var shop = Shop.Create(new ShopId(674), "Weimann - Wunsch", new Url("https://weimann-wunsch.example"), new Url("https://weimann-wunsch.example/logo.png"), 3.2);

        var loaded = ShopDocumentAdapter.ToDomain(ShopDocumentAdapter.ToDocument(shop));

        Assert.Equal(674, loaded.Id.Value);
        Assert.Equal("Weimann - Wunsch", loaded.Name);
        Assert.Equal("https://weimann-wunsch.example", loaded.Url.Value);
        Assert.Equal("https://weimann-wunsch.example/logo.png", loaded.Logo?.Value);
        Assert.Equal(3.2, loaded.Rating);
    }

    [Fact]
    public void Round_trips_without_a_logo()
    {
        var shop = Shop.Create(new ShopId(1), "Carroll Inc", new Url("https://carroll.example"));

        var loaded = ShopDocumentAdapter.ToDomain(ShopDocumentAdapter.ToDocument(shop));

        Assert.Null(loaded.Logo);
    }
}
