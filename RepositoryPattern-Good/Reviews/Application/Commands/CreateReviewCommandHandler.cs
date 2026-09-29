using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Application.Commands;

public sealed class CreateReviewCommandHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateReviewCommand>
{
    public async Task Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = Review.Create(
            command.Id, command.ProductId, command.Type, command.CreationDate,
            command.Title, command.Text, command.Score, command.ReviewUserId);

        reviewRepository.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
