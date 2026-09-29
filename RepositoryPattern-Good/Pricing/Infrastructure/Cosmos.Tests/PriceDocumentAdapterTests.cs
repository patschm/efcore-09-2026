using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.Pricing.Infrastructure.Cosmos.Adapters;
using WebShop.Pricing.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.Cosmos.Tests;

public class PriceDocumentAdapterTests
{
    private static Price SamplePrice() => Price.Create(
        new PriceId(97189), new ProductId(112), new ShopId(674), new Money(67, "EUR"), new Money(67, "EUR"), inStock: 17);

    [Fact]
    public void ToDocument_denormalizes_the_shops_name_logo_and_rating()
    {
        var shop = Shop.Create(new ShopId(674), "Weimann - Wunsch", new Url("https://x"), new Url("https://x/logo.png"), 3.2);

        var document = PriceDocumentAdapter.ToDocument(SamplePrice(), shop);

        Assert.Equal("Weimann - Wunsch", document.ShopName);
        Assert.Equal("https://x/logo.png", document.ShopLogo);
        Assert.Equal(3.2, document.ShopRating);
    }

    [Fact]
    public void ToDocument_leaves_the_shop_snapshot_empty_when_the_shop_is_unknown()
    {
        var document = PriceDocumentAdapter.ToDocument(SamplePrice(), shop: null);

        Assert.Null(document.ShopName);
        Assert.Null(document.ShopLogo);
        Assert.Null(document.ShopRating);
    }

    [Fact]
    public void Round_trips_the_price_and_shipping_amounts_and_currency()
    {
        var loaded = PriceDocumentAdapter.ToDomain(PriceDocumentAdapter.ToDocument(SamplePrice(), shop: null));

        Assert.Equal(112, loaded.ProductId.Value);
        Assert.Equal(674, loaded.ShopId.Value);
        Assert.Equal(67, loaded.ShopPrice.Amount);
        Assert.Equal("EUR", loaded.ShopPrice.Currency);
        Assert.Equal(67, loaded.ShippingPrice.Amount);
        Assert.Equal(17, loaded.InStock);
    }

    [Fact]
    public void BuildId_is_keyed_by_shop_not_by_price()
    {
        Assert.Equal("price|674", PriceDocument.BuildId(674));
    }
}
