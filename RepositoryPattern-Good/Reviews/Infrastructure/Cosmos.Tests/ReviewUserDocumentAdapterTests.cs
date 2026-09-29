using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.Cosmos.Adapters;

namespace WebShop.Reviews.Infrastructure.Cosmos.Tests;

public class ReviewUserDocumentAdapterTests
{
    [Fact]
    public void Round_trips_including_email()
    {
        var reviewUser = ReviewUser.Create(new ReviewUserId(5800), "Jan de Vries", "jan@example.com");

        var loaded = ReviewUserDocumentAdapter.ToDomain(ReviewUserDocumentAdapter.ToDocument(reviewUser));

        Assert.Equal(5800, loaded.Id.Value);
        Assert.Equal("Jan de Vries", loaded.Name);
        Assert.Equal("jan@example.com", loaded.Email);
    }

    [Fact]
    public void Round_trips_without_an_email()
    {
        var reviewUser = ReviewUser.Create(new ReviewUserId(1), "Anonymous");

        var loaded = ReviewUserDocumentAdapter.ToDomain(ReviewUserDocumentAdapter.ToDocument(reviewUser));

        Assert.Null(loaded.Email);
    }
}
