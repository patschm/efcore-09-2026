using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Commands;

public class CreateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_anonymous_review_and_saves()
    {
        var repository = new FakeReviewRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateReviewCommandHandler(repository, unitOfWork);

        await handler.Handle(
            new CreateReviewCommand(new ReviewId(1), new ProductId(1), "Video", DateOnly.FromDateTime(DateTime.Today)), default);

        var review = await repository.GetById(new ReviewId(1), default);
        Assert.NotNull(review);
        Assert.Null(review.ReviewUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_throws_when_score_is_out_of_range()
    {
        var handler = new CreateReviewCommandHandler(new FakeReviewRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(
            new CreateReviewCommand(new ReviewId(1), new ProductId(1), "Written", DateOnly.FromDateTime(DateTime.Today), Score: 10.5m), default));
    }
}
