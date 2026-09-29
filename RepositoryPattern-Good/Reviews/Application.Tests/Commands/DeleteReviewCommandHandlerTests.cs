using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Commands;

public class DeleteReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_removes_an_existing_review_and_returns_true()
    {
        var repository = new FakeReviewRepository();
        repository.Add(Review.Create(new ReviewId(1), new ProductId(1), "Video", DateOnly.FromDateTime(DateTime.Today)));
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteReviewCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new DeleteReviewCommand(new ReviewId(1)), default);

        Assert.True(result);
        Assert.Null(await repository.GetById(new ReviewId(1), default));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_false_when_review_does_not_exist()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteReviewCommandHandler(new FakeReviewRepository(), unitOfWork);

        var result = await handler.Handle(new DeleteReviewCommand(new ReviewId(999)), default);

        Assert.False(result);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
