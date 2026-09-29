using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Pricing.Infrastructure.SqlServer.Persistence.Contexts;

public sealed class PricingUnitOfWork(PricingContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
