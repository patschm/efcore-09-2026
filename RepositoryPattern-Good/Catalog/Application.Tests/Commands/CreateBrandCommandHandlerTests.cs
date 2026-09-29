using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Commands;

public class CreateBrandCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_brand_and_saves()
    {
        var repository = new FakeBrandRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateBrandCommandHandler(repository, unitOfWork);

        await handler.Handle(new CreateBrandCommand(new BrandId(1), "Samsung", "https://www.samsung.com"), default);

        var brand = await repository.GetById(new BrandId(1), default);
        Assert.NotNull(brand);
        Assert.Equal("Samsung", brand.Name);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_throws_when_website_is_not_a_valid_url()
    {
        var handler = new CreateBrandCommandHandler(new FakeBrandRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new CreateBrandCommand(new BrandId(1), "Samsung", "not-a-url"), default));
    }
}
