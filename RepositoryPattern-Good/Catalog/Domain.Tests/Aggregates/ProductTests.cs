using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;

namespace WebShop.Catalog.Domain.Tests.Aggregates;

public class ProductTests
{
    [Fact]
    public void Create_raises_ProductCreated()
    {
        var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1));

        var domainEvent = Assert.Single(product.DomainEvents);
        var productCreated = Assert.IsType<ProductCreated>(domainEvent);
        Assert.Equal(product.Id, productCreated.ProductId);
    }

    [Fact]
    public void SetSpecificationValue_adds_a_new_value_and_raises_ProductSpecificationValueSet()
    {
        var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1));
        product.ClearDomainEvents();
        var value = SpecificationValue.Create(number: 55m).Value;

        product.SetSpecificationValue(new ProductSpecificationValueId(1), new SpecificationDefinitionId(1), value);

        Assert.Single(product.SpecificationValues);
        var domainEvent = Assert.Single(product.DomainEvents);
        Assert.IsType<ProductSpecificationValueSet>(domainEvent);
    }

    [Fact]
    public void SetSpecificationValue_updates_the_existing_value_for_the_same_definition()
    {
        var product = Product.Create(new ProductId(1), "Samsung QLED 55\"", new BrandId(1));
        var definitionId = new SpecificationDefinitionId(1);
        product.SetSpecificationValue(new ProductSpecificationValueId(1), definitionId, SpecificationValue.Create(number: 55m).Value);

        product.SetSpecificationValue(new ProductSpecificationValueId(2), definitionId, SpecificationValue.Create(number: 65m).Value);

        var value = Assert.Single(product.SpecificationValues);
        Assert.Equal(65m, value.Value.Number);
    }

    [Fact]
    public void Create_throws_when_name_is_missing()
    {
        Assert.Throws<ArgumentException>(() => Product.Create(new ProductId(1), "", new BrandId(1)));
    }
}
