using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Tests.Queries;

public class GetProductsByProductGroupQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_products_with_resolved_brand_names_and_the_total_count()
    {
        var productRepository = new FakeProductRepository();
        var brandRepository = new FakeBrandRepository();
        brandRepository.Add(Brand.Create(new BrandId(1), "Samsung"));
        productRepository.Add(Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1), new ProductGroupId(1)));
        productRepository.Add(Product.Create(new ProductId(2), "Other group product", new BrandId(1), new ProductGroupId(2)));
        var handler = new GetProductsByProductGroupQueryHandler(productRepository, brandRepository);

        var result = await handler.Handle(new GetProductsByProductGroupQuery(new ProductGroupId(1)), default);

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal("Samsung", item.BrandName);
    }

    [Fact]
    public async Task Handle_pages_the_results_and_computes_total_pages()
    {
        var productRepository = new FakeProductRepository();
        var brandRepository = new FakeBrandRepository();
        brandRepository.Add(Brand.Create(new BrandId(1), "Samsung"));
        for (var id = 1; id <= 5; id++)
            productRepository.Add(Product.Create(new ProductId(id), $"Product {id}", new BrandId(1), new ProductGroupId(1)));
        var handler = new GetProductsByProductGroupQueryHandler(productRepository, brandRepository);

        var result = await handler.Handle(new GetProductsByProductGroupQuery(new ProductGroupId(1), Page: 1, PageSize: 2), default);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Handle_resolves_unknown_brand_as_a_placeholder()
    {
        var productRepository = new FakeProductRepository();
        productRepository.Add(Product.Create(new ProductId(1), "Mystery product", new BrandId(999), new ProductGroupId(1)));
        var handler = new GetProductsByProductGroupQueryHandler(productRepository, new FakeBrandRepository());

        var result = await handler.Handle(new GetProductsByProductGroupQuery(new ProductGroupId(1)), default);

        var item = Assert.Single(result.Items);
        Assert.Equal("Unknown", item.BrandName);
    }
}
