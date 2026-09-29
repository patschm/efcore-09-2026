using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Reviews.Application.Queries;

public sealed class GetReviewByIdQueryHandler(IReviewRepository reviewRepository, IReviewUserRepository reviewUserRepository)
    : IQueryHandler<GetReviewByIdQuery, ReviewDto?>
{
    public async Task<ReviewDto?> Handle(GetReviewByIdQuery query, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetById(query.Id, cancellationToken);
        if (review is null)
            return null;

        var reviewUser = review.ReviewUserId is { } reviewUserId
            ? await reviewUserRepository.GetById(reviewUserId, cancellationToken)
            : null;

        return new ReviewDto(
            review.Id.Value,
            review.ProductId.Value,
            review.Type,
            review.CreationDate,
            review.Title,
            review.Text,
            review.Score,
            review.ReviewUserId?.Value,
            reviewUser?.Name);
    }
}
