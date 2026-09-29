using WebShop.Pricing.Application.Commands;
using WebShop.Pricing.Application.Tests.Fakes;
using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Tests.Commands;

public class CreateShopCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_shop_and_saves()
    {
        var repository = new FakeShopRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateShopCommandHandler(repository, unitOfWork);

        await handler.Handle(new CreateShopCommand(new ShopId(1), "Coolblue", "https://www.coolblue.nl", Rating: 4.5), default);

        var shop = await repository.GetById(new ShopId(1), default);
        Assert.NotNull(shop);
        Assert.Equal(4.5, shop.Rating);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_throws_when_rating_is_out_of_range()
    {
        var handler = new CreateShopCommandHandler(new FakeShopRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new CreateShopCommand(new ShopId(1), "Coolblue", "https://www.coolblue.nl", Rating: 5.5), default));
    }
}
