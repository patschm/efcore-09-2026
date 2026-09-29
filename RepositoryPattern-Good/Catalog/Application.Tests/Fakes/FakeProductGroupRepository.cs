using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Tests.Fakes;

internal sealed class FakeProductGroupRepository : IProductGroupRepository
{
    private readonly Dictionary<ProductGroupId, ProductGroup> _productGroups = [];

    public Task<ProductGroup?> GetById(ProductGroupId id, CancellationToken cancellationToken) =>
        Task.FromResult(_productGroups.GetValueOrDefault(id));

    public Task<IReadOnlyList<ProductGroup>> GetByParentId(ProductGroupId? parentId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductGroup>>(_productGroups.Values.Where(g => g.ParentId == parentId).ToList());

    public void Add(ProductGroup productGroup) => _productGroups[productGroup.Id] = productGroup;
}
