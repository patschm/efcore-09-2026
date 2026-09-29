using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;

public sealed class BrandRepository(CatalogContext context) : IBrandRepository
{
    public Task<Brand?> GetById(BrandId id, CancellationToken cancellationToken) =>
        context.Brands.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Brand>> GetByIds(IReadOnlyCollection<BrandId> ids, CancellationToken cancellationToken) =>
        await context.Brands.Where(b => ids.Contains(b.Id)).ToListAsync(cancellationToken);

    public void Add(Brand brand) => context.Brands.Add(brand);
}
