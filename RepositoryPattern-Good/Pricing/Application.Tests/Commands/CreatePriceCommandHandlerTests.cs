using WebShop.Pricing.Application.Commands;
using WebShop.Pricing.Application.Tests.Fakes;
using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Tests.Commands;

public class CreatePriceCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_price_and_saves()
    {
        var repository = new FakePriceRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreatePriceCommandHandler(repository, unitOfWork);

        await handler.Handle(new CreatePriceCommand(
            new PriceId(1), new ProductId(1), new ShopId(1),
            199.99, "EUR", 204.99, "EUR", InStock: 10), default);

        var price = await repository.GetById(new PriceId(1), default);
        Assert.NotNull(price);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_throws_when_shipping_currency_does_not_match_shop_currency()
    {
        var handler = new CreatePriceCommandHandler(new FakePriceRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CreatePriceCommand(
            new PriceId(1), new ProductId(1), new ShopId(1),
            199.99, "EUR", 204.99, "USD", InStock: 10), default));
    }
}
