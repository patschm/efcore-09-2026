using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Domain.Tests.Aggregates;

public class ShopTests
{
    private static readonly Url ShopUrl = new("https://www.coolblue.nl");

    [Theory]
    [InlineData(0)]
    [InlineData(2.5)]
    [InlineData(5)]
    public void Create_accepts_ratings_within_0_to_5(double rating)
    {
        var shop = Shop.Create(new ShopId(1), "Coolblue", ShopUrl, rating: rating);

        Assert.Equal(rating, shop.Rating);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void Create_throws_when_rating_is_out_of_range(double rating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Shop.Create(new ShopId(1), "Coolblue", ShopUrl, rating: rating));
    }

    [Fact]
    public void Create_throws_when_name_is_missing()
    {
        Assert.Throws<ArgumentException>(() => Shop.Create(new ShopId(1), "", ShopUrl));
    }
}
