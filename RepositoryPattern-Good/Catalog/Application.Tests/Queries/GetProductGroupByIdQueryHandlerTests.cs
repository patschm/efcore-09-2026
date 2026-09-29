using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Queries;

public class GetProductGroupByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_null_when_group_does_not_exist()
    {
        var handler = new GetProductGroupByIdQueryHandler(new FakeProductGroupRepository());

        var dto = await handler.Handle(new GetProductGroupByIdQuery(new ProductGroupId(999)), default);

        Assert.Null(dto);
    }

    [Fact]
    public async Task Handle_maps_group_to_a_dto()
    {
        var repository = new FakeProductGroupRepository();
        repository.Add(ProductGroup.Create(new ProductGroupId(2), "Televisions", new ProductGroupId(1)));
        var handler = new GetProductGroupByIdQueryHandler(repository);

        var dto = await handler.Handle(new GetProductGroupByIdQuery(new ProductGroupId(2)), default);

        Assert.NotNull(dto);
        Assert.Equal("Televisions", dto.Name);
        Assert.Equal(1, dto.ParentId);
    }
}
