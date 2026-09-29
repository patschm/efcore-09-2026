using WebShop.Pricing.Application.Queries;
using WebShop.Pricing.Application.Tests.Fakes;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Application.Tests.Queries;

public class GetPricesByProductIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_prices_for_the_requested_product_with_resolved_shop_details()
    {
        var priceRepository = new FakePriceRepository();
        priceRepository.Add(Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1), new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10));
        priceRepository.Add(Price.Create(
            new PriceId(2), new ProductId(2), new ShopId(1), new Money(99.99, "EUR"), new Money(104.99, "EUR"), inStock: 3));
        var shopRepository = new FakeShopRepository();
        shopRepository.Add(Shop.Create(new ShopId(1), "Coolblue", new Url("https://www.coolblue.nl"), rating: 4.2));
        var handler = new GetPricesByProductIdQueryHandler(priceRepository, shopRepository);

        var prices = await handler.Handle(new GetPricesByProductIdQuery(new ProductId(1)), default);

        var price = Assert.Single(prices);
        Assert.Equal(1, price.Id);
        Assert.Equal("Coolblue", price.ShopName);
        Assert.Equal(4.2, price.ShopRating);
    }

    [Fact]
    public async Task Handle_resolves_unknown_shop_as_a_placeholder_with_zero_rating()
    {
        var priceRepository = new FakePriceRepository();
        priceRepository.Add(Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(999), new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10));
        var handler = new GetPricesByProductIdQueryHandler(priceRepository, new FakeShopRepository());

        var prices = await handler.Handle(new GetPricesByProductIdQuery(new ProductId(1)), default);

        var price = Assert.Single(prices);
        Assert.Equal("Unknown", price.ShopName);
        Assert.Equal(0, price.ShopRating);
    }
}
