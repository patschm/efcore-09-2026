using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;

namespace WebShop.Pricing.Domain.Tests.Aggregates;

public class PriceTests
{
    [Fact]
    public void Create_throws_when_shipping_currency_does_not_match_shop_currency()
    {
        Assert.Throws<ArgumentException>(() => Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1),
            new Money(199.99, "EUR"), new Money(204.99, "USD"), inStock: 10));
    }

    [Fact]
    public void TotalPrice_is_the_shipping_price_not_shop_price_plus_shipping_price()
    {
        var price = Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1),
            new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10);

        Assert.Equal(new Money(204.99, "EUR"), price.TotalPrice);
    }

    [Fact]
    public void Create_throws_when_stock_is_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1),
            new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: -1));
    }
}
