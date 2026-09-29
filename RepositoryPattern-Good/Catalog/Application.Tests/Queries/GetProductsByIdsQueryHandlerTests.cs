using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Queries;

public class GetProductsByIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_summaries_with_resolved_brand_names_for_the_requested_ids()
    {
        var productRepository = new FakeProductRepository();
        var brandRepository = new FakeBrandRepository();
        brandRepository.Add(Brand.Create(new BrandId(1), "Samsung"));
        brandRepository.Add(Brand.Create(new BrandId(2), "Sony"));
        productRepository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1)));
        productRepository.Add(Product.Create(new ProductId(2), "Sony Bravia 65\"", new BrandId(2)));
        productRepository.Add(Product.Create(new ProductId(3), "Not requested", new BrandId(1)));
        var handler = new GetProductsByIdsQueryHandler(productRepository, brandRepository);

        var result = await handler.Handle(new GetProductsByIdsQuery([new ProductId(1), new ProductId(2)]), default);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == 1 && p.BrandName == "Samsung");
        Assert.Contains(result, p => p.Id == 2 && p.BrandName == "Sony");
    }

    [Fact]
    public async Task Handle_returns_empty_list_when_no_ids_match()
    {
        var handler = new GetProductsByIdsQueryHandler(new FakeProductRepository(), new FakeBrandRepository());

        var result = await handler.Handle(new GetProductsByIdsQuery([new ProductId(999)]), default);

        Assert.Empty(result);
    }
}
