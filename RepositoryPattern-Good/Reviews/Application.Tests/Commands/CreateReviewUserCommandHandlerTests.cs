using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Commands;

public class CreateReviewUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_review_user_and_saves()
    {
        var repository = new FakeReviewUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateReviewUserCommandHandler(repository, unitOfWork);

        await handler.Handle(new CreateReviewUserCommand(new ReviewUserId(1), "Alice"), default);

        var user = await repository.GetById(new ReviewUserId(1), default);
        Assert.NotNull(user);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
