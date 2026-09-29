using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Tests.Fakes;

internal sealed class FakeBrandRepository : IBrandRepository
{
    private readonly Dictionary<BrandId, Brand> _brands = [];

    public Task<Brand?> GetById(BrandId id, CancellationToken cancellationToken) =>
        Task.FromResult(_brands.GetValueOrDefault(id));

    public Task<IReadOnlyList<Brand>> GetByIds(IReadOnlyCollection<BrandId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Brand>>(_brands.Values.Where(b => ids.Contains(b.Id)).ToList());

    public void Add(Brand brand) => _brands[brand.Id] = brand;
}
