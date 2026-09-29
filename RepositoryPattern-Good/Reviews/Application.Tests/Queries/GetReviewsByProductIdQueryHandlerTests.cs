using WebShop.Reviews.Application.Queries;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Queries;

public class GetReviewsByProductIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_reviews_for_the_requested_product_with_resolved_reviewer_names()
    {
        var reviewRepository = new FakeReviewRepository();
        var today = DateOnly.FromDateTime(DateTime.Today);
        reviewRepository.Add(Review.Create(
            new ReviewId(1), new ProductId(1), "Written", today, reviewUserId: new ReviewUserId(1)));
        reviewRepository.Add(Review.Create(new ReviewId(2), new ProductId(2), "Written", today));
        var reviewUserRepository = new FakeReviewUserRepository();
        reviewUserRepository.Add(ReviewUser.Create(new ReviewUserId(1), "Alice"));
        var handler = new GetReviewsByProductIdQueryHandler(reviewRepository, reviewUserRepository);

        var reviews = await handler.Handle(new GetReviewsByProductIdQuery(new ProductId(1)), default);

        var review = Assert.Single(reviews);
        Assert.Equal(1, review.Id);
        Assert.Equal("Alice", review.ReviewerName);
    }

    [Fact]
    public async Task Handle_leaves_reviewer_name_null_for_anonymous_reviews()
    {
        var reviewRepository = new FakeReviewRepository();
        reviewRepository.Add(Review.Create(new ReviewId(1), new ProductId(1), "Written", DateOnly.FromDateTime(DateTime.Today)));
        var handler = new GetReviewsByProductIdQueryHandler(reviewRepository, new FakeReviewUserRepository());

        var reviews = await handler.Handle(new GetReviewsByProductIdQuery(new ProductId(1)), default);

        var review = Assert.Single(reviews);
        Assert.Null(review.ReviewUserId);
        Assert.Null(review.ReviewerName);
    }
}
