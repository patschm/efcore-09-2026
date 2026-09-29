using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Queries;

public class GetProductGroupsQueryHandlerTests
{
    [Fact]
    public async Task Handle_with_null_parent_returns_only_top_level_groups()
    {
        var repository = new FakeProductGroupRepository();
        repository.Add(ProductGroup.Create(new ProductGroupId(1), "Electronics"));
        repository.Add(ProductGroup.Create(new ProductGroupId(2), "Televisions", new ProductGroupId(1)));
        var handler = new GetProductGroupsQueryHandler(repository);

        var groups = await handler.Handle(new GetProductGroupsQuery(null), default);

        var group = Assert.Single(groups);
        Assert.Equal(1, group.Id);
    }

    [Fact]
    public async Task Handle_with_a_parent_returns_only_its_children()
    {
        var repository = new FakeProductGroupRepository();
        repository.Add(ProductGroup.Create(new ProductGroupId(1), "Electronics"));
        repository.Add(ProductGroup.Create(new ProductGroupId(2), "Televisions", new ProductGroupId(1)));
        var handler = new GetProductGroupsQueryHandler(repository);

        var groups = await handler.Handle(new GetProductGroupsQuery(new ProductGroupId(1)), default);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.Id);
    }
}
