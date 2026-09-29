using WebShop.Pricing.Application.Queries;
using WebShop.Pricing.Application.Tests.Fakes;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;

namespace WebShop.Pricing.Application.Tests.Queries;

public class GetLowestPricesByProductIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_lowest_shop_price_per_product()
    {
        var repository = new FakePriceRepository();
        repository.Add(Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1), new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10));
        repository.Add(Price.Create(
            new PriceId(2), new ProductId(1), new ShopId(2), new Money(179.99, "EUR"), new Money(184.99, "EUR"), inStock: 5));
        repository.Add(Price.Create(
            new PriceId(3), new ProductId(2), new ShopId(1), new Money(49.99, "EUR"), new Money(54.99, "EUR"), inStock: 3));
        var handler = new GetLowestPricesByProductIdsQueryHandler(repository);

        var result = await handler.Handle(new GetLowestPricesByProductIdsQuery([new ProductId(1), new ProductId(2)]), default);

        Assert.Equal(179.99, result[1].Amount);
        Assert.Equal("EUR", result[1].Currency);
        Assert.Equal(49.99, result[2].Amount);
    }

    [Fact]
    public async Task Handle_omits_products_with_no_prices()
    {
        var handler = new GetLowestPricesByProductIdsQueryHandler(new FakePriceRepository());

        var result = await handler.Handle(new GetLowestPricesByProductIdsQuery([new ProductId(1)]), default);

        Assert.Empty(result);
    }
}
