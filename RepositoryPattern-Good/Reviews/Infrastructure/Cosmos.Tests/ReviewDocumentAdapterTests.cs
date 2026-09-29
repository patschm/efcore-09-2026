using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.Cosmos.Adapters;

namespace WebShop.Reviews.Infrastructure.Cosmos.Tests;

public class ReviewDocumentAdapterTests
{
    private static Review SampleReview() => Review.Create(
        new ReviewId(7656), new ProductId(112), "UserReview", new DateOnly(2008, 3, 27),
        "Degelijk", "Sterk, betrouwbaar.", score: 4m, reviewUserId: new ReviewUserId(5800));

    [Fact]
    public void ToDocument_denormalizes_the_reviewers_name_and_email()
    {
        var reviewUser = ReviewUser.Create(new ReviewUserId(5800), "Jan de Vries", "jan@example.com");

        var document = ReviewDocumentAdapter.ToDocument(SampleReview(), reviewUser);

        Assert.NotNull(document.Reviewer);
        Assert.Equal("Jan de Vries", document.Reviewer!.Name);
        Assert.Equal("jan@example.com", document.Reviewer!.Email);
    }

    [Fact]
    public void ToDocument_leaves_the_reviewer_snapshot_null_when_the_reviewer_is_unknown()
    {
        var document = ReviewDocumentAdapter.ToDocument(SampleReview(), reviewUser: null);

        Assert.Null(document.Reviewer);
    }

    [Fact]
    public void ToDocument_is_never_deleted_for_a_freshly_written_review()
    {
        Assert.False(ReviewDocumentAdapter.ToDocument(SampleReview(), reviewUser: null).IsDeleted);
    }

    [Fact]
    public void Round_trips_the_creation_date_type_title_text_score_and_reviewer_id()
    {
        var loaded = ReviewDocumentAdapter.ToDomain(ReviewDocumentAdapter.ToDocument(SampleReview(), reviewUser: null));

        Assert.Equal(112, loaded.ProductId.Value);
        Assert.Equal("UserReview", loaded.Type);
        Assert.Equal(new DateOnly(2008, 3, 27), loaded.CreationDate);
        Assert.Equal("Degelijk", loaded.Title);
        Assert.Equal("Sterk, betrouwbaar.", loaded.Text);
        Assert.Equal(4m, loaded.Score);
        Assert.Equal(5800, loaded.ReviewUserId?.Value);
    }

    [Fact]
    public void Round_trips_an_anonymous_review_with_no_reviewer_id()
    {
        var review = Review.Create(new ReviewId(1), new ProductId(1), "UserReview", new DateOnly(2020, 1, 1));

        var loaded = ReviewDocumentAdapter.ToDomain(ReviewDocumentAdapter.ToDocument(review, reviewUser: null));

        Assert.Null(loaded.ReviewUserId);
    }
}
