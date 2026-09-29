using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ACME.DataLayer.Repository.SqlServer;

public class ProductGroupRepository : BaseRepository<ProductGroup>, IProductGroupRepository
{
    public ProductGroupRepository(ShopDatabaseContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetProductsAsync(long productGroupId, int page = 1, int count = 10)
    {
        return await Context.Products
            .Where(g=>g.ProductGroupId == productGroupId)
            .Skip((page - 1) * count)
            .Take(count)
            .ToListAsync();
    }
    public async Task<IEnumerable<SpecificationDefinition>> GetSpecificationDefinitionsAsync(long productGroupId)
    {
        return await Context.SpecificationDefinitions
            .Where(g => g.ProductGroupId == productGroupId)
            .ToListAsync();
    }
}
