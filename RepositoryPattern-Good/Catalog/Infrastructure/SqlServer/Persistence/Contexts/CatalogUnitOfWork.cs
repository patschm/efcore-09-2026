using WebShop.BuildingBlocks.Application.Abstractions.Persistence;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;

public sealed class CatalogUnitOfWork(CatalogContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
