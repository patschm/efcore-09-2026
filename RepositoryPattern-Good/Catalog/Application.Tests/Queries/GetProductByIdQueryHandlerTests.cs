using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Application.Tests.Fakes;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;

namespace WebShop.Catalog.Application.Tests.Queries;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_null_when_product_does_not_exist()
    {
        var handler = new GetProductByIdQueryHandler(new FakeProductRepository(), new FakeProductGroupRepository(), new FakeBrandRepository());

        var dto = await handler.Handle(new GetProductByIdQuery(new ProductId(999)), default);

        Assert.Null(dto);
    }

    [Fact]
    public async Task Handle_maps_product_and_its_specification_values_to_a_dto()
    {
        var productRepository = new FakeProductRepository();
        var productGroupRepository = new FakeProductGroupRepository();
        var brandRepository = new FakeBrandRepository();
        var productGroup = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        productGroup.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size", type: "number", unit: "cm");
        productGroupRepository.Add(productGroup);
        brandRepository.Add(Brand.Create(new BrandId(1), "Samsung"));
        var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1), new ProductGroupId(1));
        product.SetSpecificationValue(new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), SpecificationValue.Create(number: 55m).Value);
        productRepository.Add(product);
        var handler = new GetProductByIdQueryHandler(productRepository, productGroupRepository, brandRepository);

        var dto = await handler.Handle(new GetProductByIdQuery(new ProductId(1)), default);

        Assert.NotNull(dto);
        Assert.Equal("Samsung QLED 55\"", dto.Name);
        Assert.Equal(1, dto.BrandId);
        Assert.Equal("Samsung", dto.BrandName);
        Assert.Equal(1, dto.ProductGroupId);
        var specification = Assert.Single(dto.Specifications);
        Assert.Equal("Screen size", specification.Name);
        Assert.Equal(new[] { "55 cm" }, specification.Values);
    }

    [Fact]
    public async Task Handle_includes_the_multiple_flag_from_the_specification_definition()
    {
        var productRepository = new FakeProductRepository();
        var productGroupRepository = new FakeProductGroupRepository();
        var brandRepository = new FakeBrandRepository();
        var productGroup = ProductGroup.Create(new ProductGroupId(1), "Phones");
        productGroup.DefineSpecification(new SpecificationDefinitionId(1), "ringtones", "Ringtones", type: "list", multiple: true);
        productGroupRepository.Add(productGroup);
        var product = Product.Create(new ProductId(1), "Nokia 3310", new BrandId(1), new ProductGroupId(1));
        product.SetSpecificationValue(new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), SpecificationValue.Create(text: "MP3").Value);
        productRepository.Add(product);
        var handler = new GetProductByIdQueryHandler(productRepository, productGroupRepository, brandRepository);

        var dto = await handler.Handle(new GetProductByIdQuery(new ProductId(1)), default);

        Assert.NotNull(dto);
        var specification = Assert.Single(dto.Specifications);
        Assert.True(specification.Multiple);
        Assert.Equal(new[] { "MP3" }, specification.Values);
    }
}
