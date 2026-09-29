using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;

namespace WebShop.Reviews.Domain.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetById(ReviewId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Review>> GetByProductId(ProductId productId, CancellationToken cancellationToken);

    // For enriching a product list with review scores in one round trip. Only products with at
    // least one scored review get an entry - "no entry" means "no score to show".
    Task<IReadOnlyDictionary<ProductId, double>> GetAverageScoresByProductIds(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken);

    void Add(Review review);
    void Remove(Review review);
}
