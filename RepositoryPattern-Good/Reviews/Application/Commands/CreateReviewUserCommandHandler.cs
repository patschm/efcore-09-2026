using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Application.Commands;

public sealed class CreateReviewUserCommandHandler(IReviewUserRepository reviewUserRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateReviewUserCommand>
{
    public async Task Handle(CreateReviewUserCommand command, CancellationToken cancellationToken)
    {
        var reviewUser = ReviewUser.Create(command.Id, command.Name, command.Email);

        reviewUserRepository.Add(reviewUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
