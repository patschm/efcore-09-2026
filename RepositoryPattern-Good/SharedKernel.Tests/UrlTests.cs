namespace WebShop.SharedKernel.Tests;

public class UrlTests
{
    [Fact]
    public void Constructor_accepts_a_syntactically_valid_absolute_url()
    {
        var url = new Url("https://www.samsung.com");

        Assert.Equal("https://www.samsung.com", url.Value);
    }

    [Theory]
    [InlineData("www.sony.nl")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Constructor_throws_when_value_is_not_an_absolute_url(string value)
    {
        Assert.Throws<ArgumentException>(() => new Url(value));
    }

    [Fact]
    public void ToString_returns_the_underlying_value()
    {
        var url = new Url("https://www.samsung.com");

        Assert.Equal("https://www.samsung.com", url.ToString());
    }

    [Fact]
    public void TryCreate_returns_a_url_for_a_valid_absolute_value()
    {
        var url = Url.TryCreate("https://www.samsung.com");

        Assert.Equal("https://www.samsung.com", url?.Value);
    }

    [Fact]
    public void TryCreate_returns_null_for_a_malformed_value_instead_of_throwing()
    {
        Assert.Null(Url.TryCreate("www.sony.nl"));
    }

    [Fact]
    public void TryCreate_returns_null_for_a_null_value()
    {
        Assert.Null(Url.TryCreate(null));
    }
}
