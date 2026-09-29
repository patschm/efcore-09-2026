using WebShop.Catalog.Domain.ValueObjects;

namespace WebShop.Catalog.Domain.Tests.ValueObjects;

public class SpecificationValueTests
{
    [Fact]
    public void Create_succeeds_with_exactly_one_shape_set()
    {
        Assert.True(SpecificationValue.Create(number: 55m).IsSuccess);
        Assert.True(SpecificationValue.Create(text: "red").IsSuccess);
        Assert.True(SpecificationValue.Create(flag: true).IsSuccess);
    }

    [Fact]
    public void Create_fails_when_nothing_is_set()
    {
        Assert.True(SpecificationValue.Create().IsFailure);
    }

    [Fact]
    public void Create_fails_when_more_than_one_shape_is_set()
    {
        Assert.True(SpecificationValue.Create(number: 55m, text: "red").IsFailure);
    }

    [Fact]
    public void Values_with_the_same_contents_are_equal()
    {
        Assert.Equal(SpecificationValue.Create(number: 55m).Value, SpecificationValue.Create(number: 55m).Value);
    }
}
