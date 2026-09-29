using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Reviews.Domain.Repositories;

namespace WebShop.Reviews.Application.Queries;

public sealed class GetReviewsByProductIdQueryHandler(IReviewRepository reviewRepository, IReviewUserRepository reviewUserRepository)
    : IQueryHandler<GetReviewsByProductIdQuery, IReadOnlyList<ReviewDto>>
{
    public async Task<IReadOnlyList<ReviewDto>> Handle(GetReviewsByProductIdQuery query, CancellationToken cancellationToken)
    {
        var reviews = await reviewRepository.GetByProductId(query.ProductId, cancellationToken);

        var reviewUserIds = reviews.Where(r => r.ReviewUserId is not null).Select(r => r.ReviewUserId!.Value).Distinct().ToList();
        var reviewUsers = await reviewUserRepository.GetByIds(reviewUserIds, cancellationToken);
        var reviewUserNamesById = reviewUsers.ToDictionary(u => u.Id, u => u.Name);

        return reviews
            .Select(review => new ReviewDto(
                review.Id.Value,
                review.ProductId.Value,
                review.Type,
                review.CreationDate,
                review.Title,
                review.Text,
                review.Score,
                review.ReviewUserId?.Value,
                review.ReviewUserId is { } reviewUserId ? reviewUserNamesById.GetValueOrDefault(reviewUserId) : null))
            .ToList();
    }
}
