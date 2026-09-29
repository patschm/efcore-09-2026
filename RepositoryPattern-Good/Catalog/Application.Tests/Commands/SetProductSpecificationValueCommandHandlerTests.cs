using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Contracts;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Commands;

public class SetProductSpecificationValueCommandHandlerTests
{
    [Fact]
    public async Task Handle_returns_failure_when_product_does_not_exist()
    {
        var handler = new SetProductSpecificationValueCommandHandler(
            new FakeProductRepository(), new FakeProductGroupRepository(), new FakeUnitOfWork(), new FakeOutbox(),
            new FakeDomainEventHandler<ProductSpecificationValueSet>());

        var result = await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Number: 55m),
            default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_when_value_shape_is_invalid()
    {
        var repository = new FakeProductRepository();
        repository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1)));
        var handler = new SetProductSpecificationValueCommandHandler(
            repository, new FakeProductGroupRepository(), new FakeUnitOfWork(), new FakeOutbox(),
            new FakeDomainEventHandler<ProductSpecificationValueSet>());

        var result = await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Number: 55m, Text: "fifty-five"),
            default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_sets_value_saves_and_dispatches_ProductSpecificationValueSet()
    {
        var repository = new FakeProductRepository();
        var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1));
        repository.Add(product);
        var unitOfWork = new FakeUnitOfWork();
        var specValueSetHandler = new FakeDomainEventHandler<ProductSpecificationValueSet>();
        var handler = new SetProductSpecificationValueCommandHandler(
            repository, new FakeProductGroupRepository(), unitOfWork, new FakeOutbox(), specValueSetHandler);

        var result = await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Number: 55m),
            default);

        Assert.True(result.IsSuccess);
        Assert.Single(product.SpecificationValues);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(specValueSetHandler.HandledEvents);
        Assert.Empty(product.DomainEvents);
    }

    [Fact]
    public async Task Handle_without_a_product_group_does_not_enqueue_an_integration_event()
    {
        var repository = new FakeProductRepository();
        repository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1)));
        var outbox = new FakeOutbox();
        var handler = new SetProductSpecificationValueCommandHandler(
            repository, new FakeProductGroupRepository(), new FakeUnitOfWork(), outbox,
            new FakeDomainEventHandler<ProductSpecificationValueSet>());

        await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Number: 55m),
            default);

        Assert.Empty(outbox.EnqueuedEvents);
    }

    [Fact]
    public async Task Handle_with_a_product_group_enqueues_a_specification_snapshot()
    {
        var productGroupRepository = new FakeProductGroupRepository();
        var productGroup = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        productGroup.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size");
        productGroupRepository.Add(productGroup);

        var repository = new FakeProductRepository();
        repository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1), new ProductGroupId(1)));
        var outbox = new FakeOutbox();
        var handler = new SetProductSpecificationValueCommandHandler(
            repository, productGroupRepository, new FakeUnitOfWork(), outbox,
            new FakeDomainEventHandler<ProductSpecificationValueSet>());

        await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Number: 55m),
            default);

        var integrationEvent = Assert.Single(outbox.EnqueuedEvents.OfType<ProductSpecificationsChangedIntegrationEvent>());
        Assert.Equal(1, integrationEvent.ProductId);
        Assert.Equal("Televisions", integrationEvent.ProductGroupName);
        var specification = Assert.Single(integrationEvent.Specifications);
        Assert.Equal("screen_size", specification.Key);
        Assert.Equal("Screen size", specification.Name);
        Assert.Equal("55", specification.DisplayValue);
    }

    [Fact]
    public async Task Handle_omits_text_valued_specifications_from_the_snapshot()
    {
        var productGroupRepository = new FakeProductGroupRepository();
        var productGroup = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        productGroup.DefineSpecification(new SpecificationDefinitionId(1), "remark", "Remark");
        productGroupRepository.Add(productGroup);

        var repository = new FakeProductRepository();
        repository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1), new ProductGroupId(1)));
        var outbox = new FakeOutbox();
        var handler = new SetProductSpecificationValueCommandHandler(
            repository, productGroupRepository, new FakeUnitOfWork(), outbox,
            new FakeDomainEventHandler<ProductSpecificationValueSet>());

        await handler.Handle(
            new SetProductSpecificationValueCommand(new ProductId(1), new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), Text: "Great for gaming"),
            default);

        var integrationEvent = Assert.Single(outbox.EnqueuedEvents.OfType<ProductSpecificationsChangedIntegrationEvent>());
        Assert.Empty(integrationEvent.Specifications);
    }
}
