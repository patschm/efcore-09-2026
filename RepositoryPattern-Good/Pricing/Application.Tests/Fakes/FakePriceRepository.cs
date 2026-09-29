using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;

namespace WebShop.Pricing.Application.Tests.Fakes;

internal sealed class FakePriceRepository : IPriceRepository
{
    private readonly Dictionary<PriceId, Price> _prices = [];

    public Task<Price?> GetById(PriceId id, CancellationToken cancellationToken) =>
        Task.FromResult(_prices.GetValueOrDefault(id));

    public Task<IReadOnlyList<Price>> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Price>>(_prices.Values.Where(p => p.ProductId == productId).ToList());

    public Task<IReadOnlyList<Price>> GetByProductIds(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Price>>(_prices.Values.Where(p => productIds.Contains(p.ProductId)).ToList());

    public void Add(Price price) => _prices[price.Id] = price;
}
