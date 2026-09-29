using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Domain.Tests.Aggregates;

public class ProductGroupTests
{
    [Fact]
    public void Create_throws_when_parent_is_itself()
    {
        Assert.Throws<ArgumentException>(() => ProductGroup.Create(new ProductGroupId(1), "Televisions", new ProductGroupId(1)));
    }

    [Fact]
    public void Create_throws_when_name_is_missing()
    {
        Assert.Throws<ArgumentException>(() => ProductGroup.Create(new ProductGroupId(1), ""));
    }

    [Fact]
    public void DefineSpecification_adds_a_new_definition()
    {
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");

        var result = group.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size", type: "number", unit: "inch");

        Assert.True(result.IsSuccess);
        Assert.Single(group.SpecificationDefinitions);
        Assert.Equal("screen_size", result.Value.Key);
    }

    [Fact]
    public void DefineSpecification_fails_on_duplicate_key()
    {
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");
        group.DefineSpecification(new SpecificationDefinitionId(1), "screen_size", "Screen size");

        var result = group.DefineSpecification(new SpecificationDefinitionId(2), "screen_size", "Screen size (again)");

        Assert.True(result.IsFailure);
        Assert.Single(group.SpecificationDefinitions);
    }

    [Fact]
    public void DefineSpecification_fails_when_key_is_missing()
    {
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");

        var result = group.DefineSpecification(new SpecificationDefinitionId(1), key: "", name: "Screen size");

        Assert.True(result.IsFailure);
        Assert.Empty(group.SpecificationDefinitions);
    }

    [Fact]
    public void DefineSpecification_fails_when_name_is_missing()
    {
        var group = ProductGroup.Create(new ProductGroupId(1), "Televisions");

        var result = group.DefineSpecification(new SpecificationDefinitionId(1), key: "screen_size", name: "");

        Assert.True(result.IsFailure);
        Assert.Empty(group.SpecificationDefinitions);
    }
}
