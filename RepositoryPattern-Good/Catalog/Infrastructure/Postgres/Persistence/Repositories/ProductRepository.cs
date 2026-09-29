using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.Postgres.Persistence.Repositories;

public sealed class ProductRepository(CatalogPgContext context) : IProductRepository
{
    public Task<Product?> GetById(ProductId id, CancellationToken cancellationToken) =>
        context.Products
            .Include(p => p.SpecificationValues)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByProductGroupId(ProductGroupId productGroupId, int page, int pageSize, CancellationToken cancellationToken) =>
        await context.Products
            .Where(p => p.ProductGroupId == productGroupId)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountByProductGroupId(ProductGroupId productGroupId, CancellationToken cancellationToken) =>
        context.Products.CountAsync(p => p.ProductGroupId == productGroupId, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIds(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        await context.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
}
