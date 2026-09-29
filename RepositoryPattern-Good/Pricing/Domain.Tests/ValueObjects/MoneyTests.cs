using WebShop.Pricing.Domain.ValueObjects;

namespace WebShop.Pricing.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_throws_when_amount_is_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1, "EUR"));
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    public void Constructor_throws_when_currency_is_not_a_3_letter_uppercase_code(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(10, currency));
    }

    [Fact]
    public void Adding_same_currency_sums_the_amounts()
    {
        var total = new Money(199.99, "EUR") + new Money(5, "EUR");

        Assert.Equal(new Money(204.99, "EUR"), total);
    }

    [Fact]
    public void Adding_different_currencies_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(199.99, "EUR") + new Money(5, "USD"));
    }
}
