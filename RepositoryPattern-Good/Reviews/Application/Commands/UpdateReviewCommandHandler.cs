using WebShop.Reviews.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Reviews.Application.Commands;

public sealed class UpdateReviewCommandHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateReviewCommand, bool>
{
    public async Task<bool> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetById(command.Id, cancellationToken);
        if (review is null)
            return false;

        review.Title = command.Title;
        review.Text = command.Text;
        review.Score = command.Score;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
