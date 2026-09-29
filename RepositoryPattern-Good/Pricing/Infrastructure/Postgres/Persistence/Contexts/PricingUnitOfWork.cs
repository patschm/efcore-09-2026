using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Pricing.Infrastructure.Postgres.Persistence.Contexts;

public sealed class PricingUnitOfWork(PricingPgContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
