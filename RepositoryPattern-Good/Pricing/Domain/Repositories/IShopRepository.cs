using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;

namespace WebShop.Pricing.Domain.Repositories;

public interface IShopRepository
{
    Task<Shop?> GetById(ShopId id, CancellationToken cancellationToken);

    // For batch-hydrating a list of prices with their shop's name/rating - one round trip
    // regardless of how many distinct shops the list references.
    Task<IReadOnlyList<Shop>> GetByIds(IReadOnlyCollection<ShopId> ids, CancellationToken cancellationToken);

    void Add(Shop shop);
}
