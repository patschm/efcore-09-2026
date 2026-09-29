using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Reviews.Domain.Repositories;

namespace WebShop.Reviews.Application.Queries;

// Keyed by plain int, not ProductId - this is the boundary where the application hands data to a
// caller (API/UI) that has no reason to know about that wrapper type.
public sealed class GetAverageScoresByProductIdsQueryHandler(IReviewRepository reviewRepository)
    : IQueryHandler<GetAverageScoresByProductIdsQuery, IReadOnlyDictionary<int, double>>
{
    public async Task<IReadOnlyDictionary<int, double>> Handle(GetAverageScoresByProductIdsQuery query, CancellationToken cancellationToken)
    {
        var averages = await reviewRepository.GetAverageScoresByProductIds(query.ProductIds, cancellationToken);
        return averages.ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value);
    }
}
