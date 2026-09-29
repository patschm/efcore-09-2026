using Microsoft.EntityFrameworkCore;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Repositories;
using WebShop.Pricing.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Pricing.Infrastructure.Postgres.Persistence.Repositories;

public sealed class PriceRepository(PricingPgContext context) : IPriceRepository
{
    public Task<Price?> GetById(PriceId id, CancellationToken cancellationToken) =>
        context.Prices.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Price>> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        await context.Prices.Where(p => p.ProductId == productId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Price>> GetByProductIds(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken) =>
        await context.Prices.Where(p => productIds.Contains(p.ProductId)).ToListAsync(cancellationToken);

    public void Add(Price price) => context.Prices.Add(price);
}
