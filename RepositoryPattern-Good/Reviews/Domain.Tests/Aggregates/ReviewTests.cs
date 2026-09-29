using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Domain.Tests.Aggregates;

public class ReviewTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Create_allows_an_anonymous_review_without_a_reviewer()
    {
        var review = Review.Create(new ReviewId(1), new ProductId(1), "Video", Today);

        Assert.Null(review.ReviewUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void Score_accepts_values_within_0_to_10(decimal score)
    {
        var review = Review.Create(new ReviewId(1), new ProductId(1), "Written", Today, score: score);

        Assert.Equal(score, review.Score);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void Score_throws_when_out_of_range(decimal score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(new ReviewId(1), new ProductId(1), "Written", Today, score: score));
    }

    [Fact]
    public void Create_throws_when_type_is_missing()
    {
        Assert.Throws<ArgumentException>(() => Review.Create(new ReviewId(1), new ProductId(1), "", Today));
    }
}
