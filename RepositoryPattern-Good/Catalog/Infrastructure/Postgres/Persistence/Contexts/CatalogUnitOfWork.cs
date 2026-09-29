using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

public sealed class CatalogUnitOfWork(CatalogPgContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
