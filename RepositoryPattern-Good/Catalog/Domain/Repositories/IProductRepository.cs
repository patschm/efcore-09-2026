using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;

namespace WebShop.Catalog.Domain.Repositories;

public interface IProductRepository
{
    // Includes SpecificationValues - callers invoking SetSpecificationValue need the
    // collection already loaded so it can find/update an existing value instead of duplicating it.
    Task<Product?> GetById(ProductId id, CancellationToken cancellationToken);

    // For browsing, not command validation - a list view doesn't need SpecificationValues loaded.
    // Page is 1-based.
    Task<IReadOnlyList<Product>> GetByProductGroupId(ProductGroupId productGroupId, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountByProductGroupId(ProductGroupId productGroupId, CancellationToken cancellationToken);

    // For rendering a caller-supplied set of products (e.g. similar-products results) in one
    // round trip instead of GetById per product - like GetByProductGroupId, no SpecificationValues.
    Task<IReadOnlyList<Product>> GetByIds(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    void Add(Product product);
}
