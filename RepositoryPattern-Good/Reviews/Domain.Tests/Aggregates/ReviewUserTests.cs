using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Domain.Tests.Aggregates;

public class ReviewUserTests
{
    [Fact]
    public void Create_allows_email_to_be_omitted()
    {
        var user = ReviewUser.Create(new ReviewUserId(1), "Alice");

        Assert.Null(user.Email);
    }

    [Fact]
    public void Create_throws_when_name_is_missing()
    {
        Assert.Throws<ArgumentException>(() => ReviewUser.Create(new ReviewUserId(1), ""));
    }
}
