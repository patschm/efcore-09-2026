using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Aggregates;

namespace WebShop.Catalog.Domain.Repositories;

public interface IBrandRepository
{
    Task<Brand?> GetById(BrandId id, CancellationToken cancellationToken);

    // For enriching a list of products with brand names in one round trip, rather than N calls.
    Task<IReadOnlyList<Brand>> GetByIds(IReadOnlyCollection<BrandId> ids, CancellationToken cancellationToken);

    void Add(Brand brand);
}
