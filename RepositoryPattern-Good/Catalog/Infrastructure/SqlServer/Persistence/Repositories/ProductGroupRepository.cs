using Microsoft.EntityFrameworkCore;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Catalog.Infrastructure.SqlServer.Persistence.Repositories;

public sealed class ProductGroupRepository(CatalogContext context) : IProductGroupRepository
{
    public Task<ProductGroup?> GetById(ProductGroupId id, CancellationToken cancellationToken) =>
        context.ProductGroups
            .Include(g => g.SpecificationDefinitions)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductGroup>> GetByParentId(ProductGroupId? parentId, CancellationToken cancellationToken) =>
        await context.ProductGroups
            .Where(g => g.ParentId == parentId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public void Add(ProductGroup productGroup) => context.ProductGroups.Add(productGroup);
}
