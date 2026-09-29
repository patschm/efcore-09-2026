using WebShop.Reviews.Application.Queries;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Queries;

public class GetReviewByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_null_when_review_does_not_exist()
    {
        var handler = new GetReviewByIdQueryHandler(new FakeReviewRepository(), new FakeReviewUserRepository());

        var dto = await handler.Handle(new GetReviewByIdQuery(new ReviewId(999)), default);

        Assert.Null(dto);
    }

    [Fact]
    public async Task Handle_maps_review_to_a_dto()
    {
        var reviewRepository = new FakeReviewRepository();
        reviewRepository.Add(Review.Create(
            new ReviewId(1), new ProductId(1), "Written", DateOnly.FromDateTime(DateTime.Today),
            title: "Great product", score: 4.5m, reviewUserId: new ReviewUserId(1)));
        var reviewUserRepository = new FakeReviewUserRepository();
        reviewUserRepository.Add(ReviewUser.Create(new ReviewUserId(1), "Jane Doe"));
        var handler = new GetReviewByIdQueryHandler(reviewRepository, reviewUserRepository);

        var dto = await handler.Handle(new GetReviewByIdQuery(new ReviewId(1)), default);

        Assert.NotNull(dto);
        Assert.Equal("Great product", dto.Title);
        Assert.Equal(4.5m, dto.Score);
        Assert.Equal(1, dto.ReviewUserId);
        Assert.Equal("Jane Doe", dto.ReviewerName);
    }
}
