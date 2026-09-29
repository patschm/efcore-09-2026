using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Commands;

public class CreateProductGroupCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_product_group_and_saves()
    {
        var repository = new FakeProductGroupRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateProductGroupCommandHandler(repository, unitOfWork);

        await handler.Handle(new CreateProductGroupCommand(new ProductGroupId(1), "Televisions"), default);

        var group = await repository.GetById(new ProductGroupId(1), default);
        Assert.NotNull(group);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
