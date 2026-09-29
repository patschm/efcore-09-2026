using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Commands;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_product_saves_and_dispatches_ProductCreated()
    {
        var repository = new FakeProductRepository();
        var unitOfWork = new FakeUnitOfWork();
        var productCreatedHandler = new FakeDomainEventHandler<ProductCreated>();
        var handler = new CreateProductCommandHandler(repository, unitOfWork, productCreatedHandler);

        await handler.Handle(new CreateProductCommand(new ProductId(1), "Samsung QLED 55\"", new BrandId(1)), default);

        var product = await repository.GetById(new ProductId(1), default);
        Assert.NotNull(product);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        var handled = Assert.Single(productCreatedHandler.HandledEvents);
        Assert.Equal(new ProductId(1), handled.ProductId);
        Assert.Empty(product.DomainEvents);
    }
}
