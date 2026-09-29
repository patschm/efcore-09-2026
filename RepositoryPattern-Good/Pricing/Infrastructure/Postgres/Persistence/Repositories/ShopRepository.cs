using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Pricing.Infrastructure.Postgres.Persistence.Repositories;

public sealed class ShopRepository(PricingPgContext context) : IShopRepository
{
    public Task<Shop?> GetById(ShopId id, CancellationToken cancellationToken) =>
        context.Shops.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Shop>> GetByIds(IReadOnlyCollection<ShopId> ids, CancellationToken cancellationToken) =>
        await context.Shops.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);

    public void Add(Shop shop) => context.Shops.Add(shop);
}
