using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Commands;

public class UpdateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_updates_title_text_and_score_and_returns_true()
    {
        var repository = new FakeReviewRepository();
        repository.Add(Review.Create(
            new ReviewId(1), new ProductId(1), "Written", DateOnly.FromDateTime(DateTime.Today), title: "Old title", text: "Old text", score: 9));
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateReviewCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new UpdateReviewCommand(new ReviewId(1), "Moderated title", "Redacted.", 1), default);

        Assert.True(result);
        var review = await repository.GetById(new ReviewId(1), default);
        Assert.Equal("Moderated title", review!.Title);
        Assert.Equal("Redacted.", review.Text);
        Assert.Equal(1, review.Score);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_false_when_review_does_not_exist()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateReviewCommandHandler(new FakeReviewRepository(), unitOfWork);

        var result = await handler.Handle(new UpdateReviewCommand(new ReviewId(999), "Title", null, null), default);

        Assert.False(result);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
