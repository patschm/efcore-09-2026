using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;

namespace WebShop.Catalog.Domain.Repositories;

public interface IProductGroupRepository
{
    // Includes SpecificationDefinitions - callers invoking DefineSpecification need the
    // collection already loaded for its duplicate-key check to see existing siblings.
    Task<ProductGroup?> GetById(ProductGroupId id, CancellationToken cancellationToken);

    // For browsing, not command validation - callers don't need SpecificationDefinitions loaded,
    // just enough to render a list. Null parentId means "top-level groups".
    Task<IReadOnlyList<ProductGroup>> GetByParentId(ProductGroupId? parentId, CancellationToken cancellationToken);

    void Add(ProductGroup productGroup);
}
