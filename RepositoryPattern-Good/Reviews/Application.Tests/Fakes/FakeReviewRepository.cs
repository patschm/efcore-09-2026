using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Repositories;

namespace WebShop.Reviews.Application.Tests.Fakes;

internal sealed class FakeReviewRepository : IReviewRepository
{
    private readonly Dictionary<ReviewId, Review> _reviews = [];

    public Task<Review?> GetById(ReviewId id, CancellationToken cancellationToken) =>
        Task.FromResult(_reviews.GetValueOrDefault(id));

    public Task<IReadOnlyList<Review>> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Review>>(_reviews.Values.Where(r => r.ProductId == productId).ToList());

    public Task<IReadOnlyDictionary<ProductId, double>> GetAverageScoresByProductIds(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken)
    {
        var averages = _reviews.Values
            .Where(r => productIds.Contains(r.ProductId) && r.Score is not null)
            .GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key, g => (double)g.Average(r => r.Score!.Value));

        return Task.FromResult<IReadOnlyDictionary<ProductId, double>>(averages);
    }

    public void Add(Review review) => _reviews[review.Id] = review;
    public void Remove(Review review) => _reviews.Remove(review.Id);
}
