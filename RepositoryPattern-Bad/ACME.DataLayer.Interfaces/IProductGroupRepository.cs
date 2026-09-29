using ACME.DataLayer.Entities;

namespace ACME.DataLayer.Interfaces;

public interface IProductGroupRepository : IRepository<ProductGroup>
{
    Task<IEnumerable<Product>> GetProductsAsync(long productGroupId, int page = 1, int count = 10);
    Task<IEnumerable<SpecificationDefinition>> GetSpecificationDefinitionsAsync(long productGroupId);
}
