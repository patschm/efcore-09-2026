using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;

namespace WebShop.Pricing.Domain.Repositories;

public interface IPriceRepository
{
    Task<Price?> GetById(PriceId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Price>> GetByProductId(ProductId productId, CancellationToken cancellationToken);

    // For finding the lowest price per product across a caller-supplied set (e.g. similar-products
    // results) in one round trip instead of GetByProductId per product.
    Task<IReadOnlyList<Price>> GetByProductIds(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken);

    void Add(Price price);
}
