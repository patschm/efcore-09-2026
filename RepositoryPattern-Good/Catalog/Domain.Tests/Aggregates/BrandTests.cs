using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Tests.Aggregates;

public class BrandTests
{
    [Fact]
    public void Create_sets_all_properties()
    {
        var website = new Url("https://www.samsung.com");
        var logo = new Url("https://www.samsung.com/logo.png");

        var brand = Brand.Create(new BrandId(1), "Samsung", website, logo);

        Assert.Equal(new BrandId(1), brand.Id);
        Assert.Equal("Samsung", brand.Name);
        Assert.Equal(website, brand.Website);
        Assert.Equal(logo, brand.Logo);
    }

    [Fact]
    public void Create_allows_website_and_logo_to_be_omitted()
    {
        var brand = Brand.Create(new BrandId(1), "Samsung");

        Assert.Null(brand.Website);
        Assert.Null(brand.Logo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_when_name_is_missing(string? name)
    {
        Assert.Throws<ArgumentException>(() => Brand.Create(new BrandId(1), name!));
    }
}
