using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;

namespace WebShop.Pricing.Application.Tests.Fakes;

internal sealed class FakeShopRepository : IShopRepository
{
    private readonly Dictionary<ShopId, Shop> _shops = [];

    public Task<Shop?> GetById(ShopId id, CancellationToken cancellationToken) =>
        Task.FromResult(_shops.GetValueOrDefault(id));

    public Task<IReadOnlyList<Shop>> GetByIds(IReadOnlyCollection<ShopId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Shop>>(_shops.Values.Where(s => ids.Contains(s.Id)).ToList());

    public void Add(Shop shop) => _shops[shop.Id] = shop;
}
