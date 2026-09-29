using WebShop.Pricing.Application.Queries;
using WebShop.Pricing.Application.Tests.Fakes;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Application.Tests.Queries;

public class GetPriceByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_null_when_price_does_not_exist()
    {
        var handler = new GetPriceByIdQueryHandler(new FakePriceRepository(), new FakeShopRepository());

        var dto = await handler.Handle(new GetPriceByIdQuery(new PriceId(999)), default);

        Assert.Null(dto);
    }

    [Fact]
    public async Task Handle_maps_price_to_a_dto()
    {
        var priceRepository = new FakePriceRepository();
        priceRepository.Add(Price.Create(
            new PriceId(1), new ProductId(1), new ShopId(1),
            new Money(199.99, "EUR"), new Money(204.99, "EUR"), inStock: 10));
        var shopRepository = new FakeShopRepository();
        shopRepository.Add(Shop.Create(new ShopId(1), "Coolblue", new Url("https://www.coolblue.nl"), rating: 4.2));
        var handler = new GetPriceByIdQueryHandler(priceRepository, shopRepository);

        var dto = await handler.Handle(new GetPriceByIdQuery(new PriceId(1)), default);

        Assert.NotNull(dto);
        Assert.Equal("Coolblue", dto.ShopName);
        Assert.Equal(4.2, dto.ShopRating);
        Assert.Equal(199.99, dto.ShopPriceAmount);
        Assert.Equal(204.99, dto.TotalPriceAmount);
        Assert.Equal(10, dto.InStock);
    }
}
