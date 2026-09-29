using Microsoft.EntityFrameworkCore;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Repositories;
using WebShop.Reviews.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Reviews.Infrastructure.Postgres.Persistence.Repositories;

public sealed class ReviewRepository(ReviewsPgContext context) : IReviewRepository
{
    public Task<Review?> GetById(ReviewId id, CancellationToken cancellationToken) =>
        context.Reviews.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        await context.Reviews.Where(r => r.ProductId == productId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<ProductId, double>> GetAverageScoresByProductIds(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken)
    {
        var averages = await context.Reviews
            .Where(r => productIds.Contains(r.ProductId) && r.Score != null)
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Average = g.Average(r => (double)r.Score!.Value) })
            .ToListAsync(cancellationToken);

        return averages.ToDictionary(a => a.ProductId, a => a.Average);
    }

    public void Add(Review review) => context.Reviews.Add(review);
    public void Remove(Review review) => context.Reviews.Remove(review);
}
