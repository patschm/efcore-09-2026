using WebShop.Catalog.Domain.Aggregates;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Tests.Fakes;

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<ProductId, Product> _products = [];

    public Task<Product?> GetById(ProductId id, CancellationToken cancellationToken) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<IReadOnlyList<Product>> GetByProductGroupId(ProductGroupId productGroupId, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(
            _products.Values
                .Where(p => p.ProductGroupId == productGroupId)
                .OrderByDescending(p => p.Id.Value)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList());

    public Task<int> CountByProductGroupId(ProductGroupId productGroupId, CancellationToken cancellationToken) =>
        Task.FromResult(_products.Values.Count(p => p.ProductGroupId == productGroupId));

    public Task<IReadOnlyList<Product>> GetByIds(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(_products.Values.Where(p => ids.Contains(p.Id)).ToList());

    public void Add(Product product) => _products[product.Id] = product;
}
