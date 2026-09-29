using WebShop.Reviews.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Application.Commands;

public sealed class DeleteReviewCommandHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteReviewCommand, bool>
{
    public async Task<bool> Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetById(command.Id, cancellationToken);
        if (review is null)
            return false;

        reviewRepository.Remove(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
