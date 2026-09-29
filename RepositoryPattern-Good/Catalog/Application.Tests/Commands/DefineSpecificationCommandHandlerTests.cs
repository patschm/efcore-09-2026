using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Commands;

public class DefineSpecificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_returns_failure_when_product_group_does_not_exist()
    {
        var handler = new DefineSpecificationCommandHandler(new FakeProductGroupRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new DefineSpecificationCommand(new ProductGroupId(1), new SpecificationDefinitionId(1), "screen_size", "Screen size"), default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_adds_specification_and_saves_when_group_exists()
    {
        var repository = new FakeProductGroupRepository();
        var unitOfWork = new FakeUnitOfWork();
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        repository.Add(group);
        var handler = new DefineSpecificationCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new DefineSpecificationCommand(new ProductGroupId(1), new SpecificationDefinitionId(1), "screen_size", "Screen size"), default);

        Assert.True(result.IsSuccess);
        Assert.Single(group.SpecificationDefinitions);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_on_duplicate_key_without_saving()
    {
        var repository = new FakeProductGroupRepository();
        var unitOfWork = new FakeUnitOfWork();
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        group.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size");
        repository.Add(group);
        var handler = new DefineSpecificationCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new DefineSpecificationCommand(new ProductGroupId(1), new SpecificationDefinitionId(2), "screen_size", "Screen size (again)"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
